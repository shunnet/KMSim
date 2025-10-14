using Snet.Windows.Controls.data;
using System.IO;
using System.Reflection;
using System.Xml.Linq;

namespace Snet.Windows.KMSim.handler
{
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
            var methods = type.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.Static |
                BindingFlags.DeclaredOnly);

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
    }
}
