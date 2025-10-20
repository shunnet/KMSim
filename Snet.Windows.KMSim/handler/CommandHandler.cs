using Snet.Core.handler;
using Snet.Utility;
using Snet.Windows.Controls.data;
using Snet.Windows.Controls.handler;
using Snet.Windows.KMSim.data;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Snet.Windows.KMSim.handler;

/// <summary>
/// 命令辅助处理类
/// 用于反射提取类中所有方法的名称、注释与参数说明
/// </summary>
public static class CommandHandler
{
    // 缓存 XML 文档，避免重复加载
    private static readonly Dictionary<string, XDocument> _xmlCache = new();

    /// <summary>
    /// 获取指定类型的所有方法（自动查找 XML 注释文件）
    /// </summary>
    /// <param name="type">目标类型</param>
    /// <returns>方法信息列表</returns>
    public static List<EditModel> Get(this Type type)
    {
        string assemblyPath = type.Assembly.Location;
        string xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
        return Get(type, xmlPath);
    }

    /// <summary>
    /// 获取指定类型的所有方法（包含注释与参数描述）
    /// </summary>
    /// <param name="type">目标类型</param>
    /// <param name="xmlPath">XML 注释文件路径</param>
    /// <param name="color">十六进制的颜色</param>
    /// <returns>方法信息列表</returns>
    public static List<EditModel> Get(this Type type, string xmlPath, string color = "#4EB2FF")
    {
        var list = new List<EditModel>();
        if (type == null) return list;

        XDocument xml = LoadXmlSafe(xmlPath);
        if (xml == null) return list;

        // 获取当前类声明的所有方法
        var methods = type.GetMethods(App.BindingAttr);

        foreach (var method in methods)
        {
            string methodName = method.Name;
            string summary = GetSummary(xml, type, method);
            string paramDesc = GetParameterDescriptions(xml, method);

            // 拼接最终描述文本
            string description = summary;
            if (!string.IsNullOrEmpty(paramDesc))
                description += "\n（ " + paramDesc + " ）";

            list.Add(new EditModel
            {
                Name = methodName,
                Color = color,
                Description = description.Trim()
            });
        }

        return list;
    }

    /// <summary>
    /// 安全加载 XML 文件并缓存
    /// </summary>
    private static XDocument? LoadXmlSafe(string xmlPath)
    {
        if (string.IsNullOrWhiteSpace(xmlPath) || !File.Exists(xmlPath))
            return null;

        if (_xmlCache.TryGetValue(xmlPath, out var cached))
            return cached;

        try
        {
            var xml = XDocument.Load(xmlPath);
            _xmlCache[xmlPath] = xml;
            return xml;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 从 XML 获取方法摘要（summary），只取第一行
    /// </summary>
    private static string GetSummary(XDocument xml, Type type, MethodInfo method)
    {
        string fullMethodName = $"M:{type.FullName}.{method.Name}";

        // 拼接参数类型（用于重载区分）
        var parameters = method.GetParameters();
        if (parameters.Length > 0)
        {
            string paramTypes = string.Join(",", parameters.Select(p => p.ParameterType.FullName));
            fullMethodName += $"({paramTypes})";
        }

        var xmlNode = xml.Descendants("member")
                         .FirstOrDefault(x => x.Attribute("name")?.Value == fullMethodName);
        if (xmlNode == null) return "";

        string summary = xmlNode.Element("summary")?.Value ?? "";
        summary = summary.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                         .FirstOrDefault()?.Trim() ?? "";
        return summary;
    }

    /// <summary>
    /// 获取方法参数描述信息，只允许 int、nint、string 类型
    /// </summary>
    private static string GetParameterDescriptions(XDocument xml, MethodInfo method)
    {
        var parameters = method.GetParameters();
        if (parameters.Length == 0) return "";

        string fullMethodName = $"M:{method.DeclaringType!.FullName}.{method.Name}";
        if (parameters.Length > 0)
        {
            string paramTypes = string.Join(",", parameters.Select(p => p.ParameterType.FullName));
            fullMethodName += $"({paramTypes})";
        }

        var xmlNode = xml.Descendants("member")
                         .FirstOrDefault(x => x.Attribute("name")?.Value == fullMethodName);
        if (xmlNode == null) return "";

        var paramNodes = xmlNode.Elements("param");
        var paramList = new List<string>();

        foreach (var p in parameters)
        {
            if (p.Name?.Equals("token", StringComparison.OrdinalIgnoreCase) == true)
                continue;

            // 过滤只允许 int、nint、string
            Type pt = p.ParameterType;
            if (pt != typeof(int) && pt != typeof(nint) && pt != typeof(string))
                continue;

            var node = paramNodes.FirstOrDefault(n => n.Attribute("name")?.Value == p.Name);
            string desc = node?.Value.Trim() ?? p.Name;

            paramList.Add($"{desc} ‹ {p.ParameterType.Name} ›");
        }

        return paramList.Count > 0 ? string.Join(" ，", paramList) : "";
    }


    /// <summary>
    /// 将文本解析为 WhileModel 集合（每个 While 块包含对应的 LogicModel 集合）
    /// 支持格式：
    ///     While = 1
    ///     While = true
    ///     MoveWindowsAsync = GetWindowsHandleAsync‹notepad aaa bbb› 0 0 1000 1000
    /// 规则说明：
    ///     1. 当参数中包含 ‹› 时，其内部内容整体视为一个参数
    ///     2. 如果开头没有 While，则自动创建一个默认循环块（LoopCount = 1, EndlessLoop = false）
    ///     3. 遇到新的 While 时，结束上一个块并开始新的块
    /// </summary>
    /// <param name="text">待解析的脚本文本</param>
    /// <returns>解析后的 WhileModel 集合</returns>
    public static List<WhileModel> Parse(this string text)
    {
        List<WhileModel> whileList = new();

        // 按行拆分并清理注释/空行
        string[] lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                             .Select(l => l.Trim())
                             .Where(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith("#"))
                             .ToArray();

        WhileModel? currentWhile = null;
        List<LogicModel> currentLogicList = new();

        foreach (var line in lines)
        {
            // 判断是否是 While 开头
            if (line.StartsWith("While", StringComparison.OrdinalIgnoreCase))
            {
                // 如果已有块，保存当前块
                if (currentWhile != null)
                {
                    currentWhile.Logics = new List<LogicModel>(currentLogicList);
                    whileList.Add(currentWhile);
                    currentLogicList.Clear();
                }

                // 创建新的 WhileModel
                currentWhile = new WhileModel();

                // 解析 While 参数
                string paramText = "";
                if (line.Contains('='))
                {
                    string[] parts = line.Split('=', 2);
                    paramText = parts.Length > 1 ? parts[1].Trim() : "";
                }

                // 参数判定：可能是 true / false / 数字
                if (bool.TryParse(paramText, out bool endless))
                {
                    currentWhile.EndlessLoop = endless;
                    currentWhile.LoopCount = endless ? 1 : 0;
                }
                else if (int.TryParse(paramText, out int count))
                {
                    currentWhile.LoopCount = count;
                    currentWhile.EndlessLoop = false;
                }
                else
                {
                    // 默认 1 次
                    currentWhile.LoopCount = 1;
                    currentWhile.EndlessLoop = false;
                }

                continue;
            }

            // 非 While 行 => 普通命令
            // 如果还没有任何 While，则自动创建一个默认块（表示前置逻辑）
            if (currentWhile == null)
            {
                currentWhile = new WhileModel
                {
                    LoopCount = 1,
                    EndlessLoop = false
                };
            }

            // 解析命令与参数
            string methodName;
            string paramText2 = "";

            if (line.Contains('='))
            {
                string[] parts = line.Split('=', 2);
                methodName = parts[0].Trim();
                paramText2 = parts.Length > 1 ? parts[1].Trim() : "";
            }
            else
            {
                methodName = line;
            }

            // 参数解析（支持 ‹› 包裹整体）
            List<string> parameters = new();
            StringBuilder current = new();
            bool inBracket = false;

            for (int i = 0; i < paramText2.Length; i++)
            {
                char c = paramText2[i];
                if (c == '‹')
                {
                    inBracket = true;
                    current.Append(c);
                }
                else if (c == '›')
                {
                    inBracket = false;
                    current.Append(c);
                }
                else if (!inBracket && (c == ' ' || c == ','))
                {
                    if (current.Length > 0)
                    {
                        parameters.Add(current.ToString());
                        current.Clear();
                    }
                }
                else
                {
                    current.Append(c);
                }
            }

            if (current.Length > 0)
                parameters.Add(current.ToString());

            currentLogicList.Add(new LogicModel
            {
                MethodName = methodName,
                Parameters = parameters.Cast<object>().ToArray()
            });
        }

        // 处理最后一个块（可能是默认块或最后的 While 块）
        if (currentWhile != null)
        {
            currentWhile.Logics = new List<LogicModel>(currentLogicList);
            whileList.Add(currentWhile);
        }

        return whileList;
    }








    // 方法缓存：Type -> 方法名 -> 参数数量 -> 方法列表<br/>
    // 提升多重重载查找性能，避免每次都遍历全部方法
    private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, ConcurrentDictionary<int, MethodInfo[]>>> MethodCache = new();

    // 类型转换缓存：Type（去除 Nullable 的基础类型） -> 转换函数<br/>
    private static readonly ConcurrentDictionary<Type, Func<object, (bool success, object? value)>> ConvertCache = new();

    /// <summary>
    /// 通过反射执行方法，支持同步/异步、默认参数、可变参数(params)与自动类型转换
    /// </summary>
    public static async Task<object?> InvokeMethodAsync(this object target, string methodName, object[]? parameters, UiMessageHandler uiMessage, CancellationToken token = default)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));

        parameters ??= Array.Empty<object>();
        var type = target.GetType();

        if (parameters != null && parameters.Length > 0)
        {
            string pattern = @"([^\s‹=]+)‹(?>[^‹›]+|(?<Open>‹)|(?<-Open>›))*(?(Open)(?!))›";
            for (int i = 0; i < parameters.Length; i++)
            {
                Match match = Regex.Match(parameters[i].ToString(), pattern);
                if (match.Success)
                {
                    // 整个匹配
                    string whole = match.Value;

                    // 找第一个 ‹ 的位置，分割方法名和内容
                    int firstBracket = whole.IndexOf('‹');
                    if (firstBracket < 0) continue;

                    string method = whole.Substring(0, firstBracket);
                    string content = whole.Substring(firstBracket + 1, whole.Length - firstBracket - 2); // 去掉最外层 ‹ ›
                    List<object> objects = new List<object>();

                    if (!content.IsNullOrWhiteSpace() && content.Contains(" "))
                    {
                        objects.AddRange(content.Split(' '));
                    }
                    else if (!content.IsNullOrWhiteSpace() && !content.Contains(" "))
                    {
                        objects.Add(content);
                    }

                    parameters[i] = await InvokeMethodAsync(target, method, objects.ToArray(), uiMessage, token);

                    try
                    {
                        if (parameters[i] != null)
                            await uiMessage.ShowAsync($"{App.LanguageOperate.GetLanguageValue("执行")} {methodName} {App.LanguageOperate.GetLanguageValue("返回")} ：{parameters[i]}");
                        else
                            await uiMessage.ShowAsync($"{App.LanguageOperate.GetLanguageValue("执行")} {methodName} {App.LanguageOperate.GetLanguageValue("成功")}");
                    }
                    catch (OperationCanceledException)
                    {
                        await uiMessage.ShowAsync($"{App.LanguageOperate.GetLanguageValue("执行")} {methodName} {App.LanguageOperate.GetLanguageValue("已取消")}");
                    }
                    catch (Exception ex)
                    {
                        await uiMessage.ShowAsync($"{App.LanguageOperate.GetLanguageValue("执行")} {methodName} {App.LanguageOperate.GetLanguageValue("出错")} ：{ex.Message}");
                    }
                }
                else
                {
                    //如果是嵌套执行命令
                    string comm = parameters[i].ToString();
                    bool status = App.EditModels.Any(c => c.Name == comm);
                    if (status)
                    {
                        parameters[i] = await InvokeMethodAsync(target, comm, null, uiMessage, token);
                    }
                }
            }
        }

        // 从缓存中获取方法列表<br/>
        var methods = GetMethodsCached(type, methodName, parameters.Length);

        MethodInfo? matched = null;
        object[]? finalArgs = null;

        foreach (var method in methods)
        {
            var paramInfos = method.GetParameters();
            int paramCount = paramInfos.Length;

            // 是否为 params 数组形式的最后一个参数<br/>
            bool hasParams = paramCount > 0 && Attribute.IsDefined(paramInfos[^1], typeof(ParamArrayAttribute));
            bool expectsToken = paramCount > 0 && paramInfos[^1].ParameterType == typeof(CancellationToken);

            var tempArgs = new object[paramCount];
            bool canConvert = true;
            if ((paramCount - 1) < parameters.Length)
            {
                string info = string.Join(" ", parameters.Select(p => p?.ToString() ?? ""));
                parameters = new object[1];
                parameters[0] = info;
            }
            int normalParamCount = hasParams ? paramCount - 1 : paramCount;

            for (int i = 0; i < normalParamCount; i++)
            {
                if (i < parameters.Length)
                {
                    if (!TryConvertParameter(parameters[i], paramInfos[i].ParameterType, out var value))
                    {
                        canConvert = false;
                        break;
                    }
                    tempArgs[i] = value!;
                }
                else if (paramInfos[i].HasDefaultValue)
                {
                    tempArgs[i] = paramInfos[i].DefaultValue is DBNull
                        ? Activator.CreateInstance(paramInfos[i].ParameterType)!
                        : paramInfos[i].DefaultValue!;
                }
                else
                {
                    tempArgs[i] = paramInfos[i].ParameterType.IsValueType
                        ? Activator.CreateInstance(paramInfos[i].ParameterType)!
                        : null!;
                }
            }

            if (!canConvert)
                continue;

            // 处理 params 参数打包<br/>
            if (hasParams)
            {
                var elementType = paramInfos[^1].ParameterType.GetElementType()!;
                var extraArgs = parameters.Skip(normalParamCount).ToArray();
                var arr = Array.CreateInstance(elementType, extraArgs.Length);

                for (int j = 0; j < extraArgs.Length; j++)
                {
                    if (!TryConvertParameter(extraArgs[j], elementType, out var value))
                    {
                        canConvert = false;
                        break;
                    }
                    arr.SetValue(value, j);
                }
                if (!canConvert) continue;
                tempArgs[^1] = arr;
            }
            else if (expectsToken && parameters.Length < paramCount)
            {
                // 仅当方法最后参数为 CancellationToken 且用户未传入时自动注入 token<br/>
                tempArgs[^1] = token;
            }

            matched = method;
            finalArgs = tempArgs;
            break;
        }

        if (matched == null)
            throw new InvalidOperationException($"{App.LanguageOperate.GetLanguageValue("未找到可匹配的重载方法")}：{methodName}");

        object? result;
        try
        {
            result = matched.Invoke(target, finalArgs);
        }
        catch (TargetInvocationException ex)
        {
            throw ex.InnerException ?? ex;
        }

        // 异步结果支持<br/>
        if (result is Task task)
        {
            await task.ConfigureAwait(false);
            var resultProperty = task.GetType().GetProperty("Result");
            object? value = resultProperty?.GetValue(task);
            if (value != null && value.GetType().FullName != "System.Threading.Tasks.VoidTaskResult")
                return value;
            return null;
        }

        return result;
    }

    /// <summary>
    /// 从缓存中获取方法列表（按 Type、方法名、参数数量 分组缓存）<br/>
    /// 这样在存在大量重载时能显著减少匹配时间<br/>
    /// </summary>
    private static MethodInfo[] GetMethodsCached(Type type, string methodName, int paramCount)
    {
        var typeCache = MethodCache.GetOrAdd(type, _ => new());
        var nameCache = typeCache.GetOrAdd(methodName, _ => new());
        return nameCache.GetOrAdd(paramCount, _ =>
            type.GetMethods(App.BindingAttr)
                .Where(m => m.Name == methodName)
                .ToArray());
    }

    /// <summary>
    /// 尝试将输入对象转换为指定类型，失败时不抛异常而返回 false<br/>
    /// 对常用类型（int、double、enum、bool、IntPtr、Nullable 等）采用缓存的高性能转换函数<br/>
    /// </summary>
    private static bool TryConvertParameter(object input, Type targetType, out object? value)
    {
        value = null;
        if (input == null)
        {
            value = targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
            return true;
        }

        if (targetType.IsAssignableFrom(input.GetType()))
        {
            value = input;
            return true;
        }

        var baseType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        var converter = ConvertCache.GetOrAdd(baseType, t => CreateConverter(t));

        var (success, converted) = converter(input);
        if (success)
        {
            value = converted;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 创建类型转换函数（返回 (bool success, object? value)），避免异常开销<br/>
    /// 支持 IntPtr、UIntPtr、枚举、基础数值类型、布尔、字符串等<br/>
    /// </summary>
    private static Func<object, (bool success, object? value)> CreateConverter(Type targetType)
    {
        if (targetType == typeof(IntPtr))
            return input =>
            {
                try
                {
                    if (input is IntPtr ptr) return (true, ptr);
                    if (input is string s)
                    {
                        s = s.Trim();
                        if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                            && long.TryParse(s[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hex))
                            return (true, new IntPtr(hex));
                        if (long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var val))
                            return (true, new IntPtr(val));
                    }
                    if (input is IConvertible c)
                        return (true, new IntPtr(Convert.ToInt64(c, CultureInfo.InvariantCulture)));
                }
                catch { }
                return (false, null);
            };

        if (targetType == typeof(UIntPtr))
            return input =>
            {
                try
                {
                    if (input is UIntPtr ptr) return (true, ptr);
                    if (input is string s)
                    {
                        s = s.Trim();
                        if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                            && ulong.TryParse(s[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hex))
                            return (true, new UIntPtr(hex));
                        if (ulong.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var val))
                            return (true, new UIntPtr(val));
                    }
                    if (input is IConvertible c)
                        return (true, new UIntPtr(Convert.ToUInt64(c, CultureInfo.InvariantCulture)));
                }
                catch { }
                return (false, null);
            };

        if (targetType.IsEnum)
            return input =>
            {
                try
                {
                    if (input is string s)
                    {
                        s = s.Trim();
                        if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                            && long.TryParse(s[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hex))
                            return (true, Enum.ToObject(targetType, hex));
                        if (long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var num))
                            return (true, Enum.ToObject(targetType, num));
                        return (true, Enum.Parse(targetType, s, true));
                    }
                    if (input is IConvertible)
                    {
                        var baseVal = Convert.ChangeType(input, Enum.GetUnderlyingType(targetType), CultureInfo.InvariantCulture);
                        return (true, Enum.ToObject(targetType, baseVal!));
                    }
                }
                catch { }
                return (false, null);
            };

        if (targetType == typeof(int))
            return input =>
            {
                if (input is string s)
                {
                    s = s.Trim();
                    if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                        && int.TryParse(s[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hex))
                        return (true, hex);
                    if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var val))
                        return (true, val);
                }
                if (input is IConvertible c)
                    return (true, Convert.ToInt32(c, CultureInfo.InvariantCulture));
                return (false, null);
            };

        if (targetType == typeof(uint))
            return input =>
            {
                if (input is string s)
                {
                    s = s.Trim();
                    if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                        && uint.TryParse(s[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hex))
                        return (true, hex);
                    if (uint.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var val))
                        return (true, val);
                }
                if (input is IConvertible c)
                    return (true, Convert.ToUInt32(c, CultureInfo.InvariantCulture));
                return (false, null);
            };

        if (targetType == typeof(double))
            return input =>
            {
                if (input is string s && double.TryParse(s.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                    return (true, d);
                if (input is IConvertible c)
                    return (true, Convert.ToDouble(c, CultureInfo.InvariantCulture));
                return (false, null);
            };

        if (targetType == typeof(bool))
            return input =>
            {
                if (input is string s && bool.TryParse(s.Trim(), out var b))
                    return (true, b);
                if (input is IConvertible c)
                    return (true, Convert.ToBoolean(c, CultureInfo.InvariantCulture));
                return (false, null);
            };

        if (targetType == typeof(string))
            return input => (true, input.ToString());

        return input =>
        {
            try
            {
                return (true, Convert.ChangeType(input, targetType, CultureInfo.InvariantCulture)!);
            }
            catch { return (false, null); }
        };
    }


}
