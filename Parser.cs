using System.Xml;
using System.Xml.Linq;

namespace MauiApp1;

public interface IXmlParseStrategy
{
    string Parse(string filePath);
}

public class SaxParseStrategy : IXmlParseStrategy
{
    public string Parse(string filePath)
    {
        var result = XmlParserContext.GetTableHeader(); 

        using (XmlReader reader = XmlReader.Create(filePath))
        {
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "Student")
                {
                    string name = reader.GetAttribute("Name");
                    string faculty = reader.GetAttribute("Faculty");
                    string department = reader.GetAttribute("Department");
                    string course = reader.GetAttribute("Course");
                    string room = reader.GetAttribute("Room");

                    result += XmlParserContext.FormatRow(name, faculty, department, course, room) + "\n";
                }
            }
        }

        return result;
    }
}

public class DomParseStrategy : IXmlParseStrategy
{
    public string Parse(string filePath)
    {
        var result = XmlParserContext.GetTableHeader(); 

        XmlDocument doc = new XmlDocument();
        doc.Load(filePath);

        XmlNodeList students = doc.GetElementsByTagName("Student");
        foreach (XmlNode student in students)
        {
            string name = student.SelectSingleNode("Name")?.InnerText ?? "";
            string faculty = student.Attributes["Faculty"]?.Value ?? "";
            string department = student.Attributes["Department"]?.Value ?? "";
            string course = student.Attributes["Course"]?.Value ?? "";
            string room = student.Attributes["Room"]?.Value ?? "";

            result += XmlParserContext.FormatRow(name, faculty, department, course, room) + "\n";
        }

        return result;
    }
}

public class LinqParseStrategy : IXmlParseStrategy
{
    public string Parse(string filePath)
    {
        var result = XmlParserContext.GetTableHeader(); 

        XDocument doc = XDocument.Load(filePath);
        var students = doc.Descendants("Student")
                          .Select(s => new
                          {
                              Name = s.Element("Name")?.Value ?? "",
                              Faculty = s.Attribute("Faculty")?.Value ?? "",
                              Department = s.Attribute("Department")?.Value ?? "",
                              Course = s.Attribute("Course")?.Value ?? "",
                              Room = s.Attribute("Room")?.Value ?? ""
                          });

        foreach (var student in students)
        {
            result += XmlParserContext.FormatRow(student.Name, student.Faculty, student.Department, student.Course, student.Room) + "\n";
        }

        return result;
    }
}

public class XmlParserContext
{
    private IXmlParseStrategy _strategy;

    public void SetStrategy(IXmlParseStrategy strategy)
    {
        _strategy = strategy;
    }

    public string ExecuteParse(string filePath)
    {
        if (_strategy == null)
        {
            throw new InvalidOperationException("Parsing strategy is not set.");
        }

        return _strategy.Parse(filePath);
    }

    // Сортування результатів
    public static string SortData(string data, string sortBy)
    {
        var lines = data.Split('\n', StringSplitOptions.RemoveEmptyEntries).Skip(2); // Пропускаємо заголовок і лінію
        var sorted = lines.OrderBy(line =>
        {
            var columns = line.Split('|');
            if (sortBy == "Name") return columns[0].Trim();
            if (sortBy == "Faculty") return columns[1].Trim();
            if (sortBy == "Room") return columns[4].Trim();
            return line;
        });

        string header = data.Split('\n', StringSplitOptions.RemoveEmptyEntries).First();
        string separator = new string('-', 80);
        return $"{header}\n{separator}\n{string.Join("\n", sorted)}";
    }

    public static string GetTableHeader()
    {
        string header = FormatRow("Name", "Faculty", "Department", "Course", "Room");
        string separator = new string('-', 80);
        return $"{header}\n{separator}\n";
    }

    public static string FormatRow(string name, string faculty, string department, string course, string room)
    {
        return $"{name,-15} | {faculty,-20} | {department,-20} | {course,-10} | {room,-5}";
    }
}