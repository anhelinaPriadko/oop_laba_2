using System.Xml.Linq;
using System.Xml.Xsl;

namespace MauiApp1;

public partial class MainPage : ContentPage
{
    private string xmlFilePath;
    private readonly XmlParserContext _parserContext = new();
    private bool _isParsed; 

    public MainPage()
    {
        InitializeComponent();

        ParserPicker.Items.Add("SAX");
        ParserPicker.Items.Add("DOM");
        ParserPicker.Items.Add("LINQ to XML");
    }

    private async Task<bool> ValidateFileAndMethodAsync()
    {
        if (xmlFilePath == null)
        {
            await DisplayAlert("Warning", "Please, select an XML file first.", "OK");
            return false;
        }

        if (ParserPicker.SelectedIndex == -1)
        {
            await DisplayAlert("Warning", "Please, select a parsing method first.", "OK");
            return false;
        }

        return true;
    }

    private void LoadFacultiesFromXml()
    {
        if (xmlFilePath == null) return;

        try
        {
            XDocument doc = XDocument.Load(xmlFilePath);

            var faculties = doc.Descendants("Student")
                               .Select(s => s.Attribute("Faculty")?.Value)
                               .Where(f => !string.IsNullOrEmpty(f))
                               .Distinct()
                               .OrderBy(f => f);

            FacultyPicker.ItemsSource = faculties.ToList();
        }
        catch (Exception ex)
        {
            DisplayAlert("Error", $"Failed to load faculties: {ex.Message}", "OK");
        }
    }

    private string ParseXmlFile()
    {
        switch (ParserPicker.SelectedIndex)
        {
            case 0:
                _parserContext.SetStrategy(new SaxParseStrategy());
                break;
            case 1:
                _parserContext.SetStrategy(new DomParseStrategy());
                break;
            case 2:
                _parserContext.SetStrategy(new LinqParseStrategy());
                break;
            default:
                throw new InvalidOperationException("Parsing method is not selected.");
        }

        string result = _parserContext.ExecuteParse(xmlFilePath);

        if (SortPicker.SelectedIndex >= 0)
        {
            string sortBy = SortPicker.SelectedItem.ToString();
            result = XmlParserContext.SortData(result, sortBy);
        }

        return result;
    }

    private string PerformSearch()
    {
        XDocument doc = XDocument.Load(xmlFilePath);
        string nameFilter = NameEntry.Text?.Trim();
        string facultyFilter = FacultyPicker.SelectedItem?.ToString();
        string roomFilter = RoomEntry.Text?.Trim();

        var students = doc.Descendants("Student")
                          .Where(s => (string.IsNullOrEmpty(nameFilter) || (s.Element("Name")?.Value ?? "").Contains(nameFilter, StringComparison.OrdinalIgnoreCase)) &&
                                      (string.IsNullOrEmpty(facultyFilter) || (s.Attribute("Faculty")?.Value ?? "").Equals(facultyFilter, StringComparison.OrdinalIgnoreCase)) &&
                                      (string.IsNullOrEmpty(roomFilter) || (s.Attribute("Room")?.Value ?? "").Contains(roomFilter, StringComparison.OrdinalIgnoreCase)))
                          .Select(s => new
                          {
                              Name = s.Element("Name")?.Value ?? "",
                              Faculty = s.Attribute("Faculty")?.Value ?? "",
                              Department = s.Attribute("Department")?.Value ?? "",
                              Course = s.Attribute("Course")?.Value ?? "",
                              Room = s.Attribute("Room")?.Value ?? ""
                          });

        string result = XmlParserContext.GetTableHeader();
        foreach (var student in students)
        {
            result += XmlParserContext.FormatRow(student.Name, student.Faculty, student.Department, student.Course, student.Room) + "\n";
        }

        return result;
    }

private void ClearAllFields()
{
    OutputEditor.Text = string.Empty;
    NameEntry.Text = string.Empty;
    FacultyPicker.SelectedIndex = -1;
    RoomEntry.Text = string.Empty;
    xmlFilePath = null;

    SortPicker.SelectedIndex = -1;
    ParserPicker.SelectedIndex = -1;
    _isParsed = false; 
}

    private async void OnParseClicked(object sender, EventArgs e)
    {
        if (!await ValidateFileAndMethodAsync())
            return;

        try
        {
            XDocument doc = XDocument.Load(xmlFilePath);

            if (!IsValidXmlStructure(doc))
            {
                await DisplayAlert("Error", "The XML file does not match the expected structure.", "OK");
                ClearAllFields();
                return; 
            }

            string result = ParseXmlFile();
            _isParsed = true; 

            string searchResult = PerformSearch();

            OutputEditor.Text = string.IsNullOrWhiteSpace(searchResult) ? "No records found." : searchResult;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to parse the XML file: {ex.Message}", "OK");
        }
    }



    private async void OnSearchClicked(object sender, EventArgs e)
    {
        if (!_isParsed)
        {
            await DisplayAlert("Warning", "Please, parse the XML file first.", "OK");
            return;
        }

        try
        {
            string result = PerformSearch();
            OutputEditor.Text = string.IsNullOrWhiteSpace(result) ? "No records found." : result;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to search in the parsed data: {ex.Message}", "OK");
        }
    }

    private void OnClearClicked(object sender, EventArgs e)
    {
        ClearAllFields();
    }

    private async void OnTransformClicked(object sender, EventArgs e)
    {
        if (xmlFilePath == null)
        {
            await DisplayAlert("Warning", "Please, select an XML file first.", "OK");
            return;
        }

        string xslFilePath = Path.Combine(AppContext.BaseDirectory, "transform.xsl");

        if (!File.Exists(xslFilePath))
        {
            await DisplayAlert("Error", "XSL file not found.", "OK");
            return;
        }

        try
        {
            XslCompiledTransform transform = new XslCompiledTransform();
            transform.Load(xslFilePath);
            string outputFile = Path.Combine(AppContext.BaseDirectory, "output.html");
            transform.Transform(xmlFilePath, outputFile);

            await DisplayAlert("Success", $"HTML file generated: {outputFile}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to transform XML to HTML: {ex.Message}", "OK");
        }
    }

    private async void OnSelectXmlFileClicked(object sender, EventArgs e)
    {
        xmlFilePath = await PickXmlFileAsync();
        if (xmlFilePath != null)
        {
            OutputEditor.Text = $"Selected XML file: {xmlFilePath}\n";
            LoadFacultiesFromXml(); 
        }
    }

    private async Task<string> PickXmlFileAsync()
    {
        try
        {
            var customXmlFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.WinUI, new[] { ".xml" } },
                { DevicePlatform.Android, new[] { "application/xml" } },
                { DevicePlatform.iOS, new[] { "public.xml" } },
            });

            var result = await FilePicker.PickAsync(new PickOptions
            {
                FileTypes = customXmlFileType,
                PickerTitle = "Select an XML file"
            });

            return result?.FullPath;
        }
        catch
        {
            return null;
        }
    }

    private bool IsValidXmlStructure(XDocument doc)
    {
        var students = doc.Descendants("Student");
        foreach (var student in students)
        {
            if (student.Element("Name") == null || student.Attribute("Faculty") == null)
            {
                return false; 
            }
        }
        return true; 
    }

    private async void OnExitClicked(object sender, EventArgs e)
    {
        bool confirmExit = await DisplayAlert("Exit", "Are you sure you want to exit?", "Yes", "No");
        if (confirmExit)
        {
            Application.Current.Quit(); 
        }
    }

}
