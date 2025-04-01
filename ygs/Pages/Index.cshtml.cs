using Microsoft.AspNetCore.Mvc;
using iText.Html2pdf;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using System.Text;
using System.Globalization;

namespace UBS_MemberData.Pages;

public class IndexModel : PageModel
{

    int associationFee = 100;

    private readonly ILogger<IndexModel> _logger;
    static DataTable dtMemberList = new DataTable();

    [BindProperty]
    public string memberName { get; set; }

    [BindProperty]
    public string houseNo { get; set; }
    [BindProperty]
    public DateTime paidDate { get; set; }
    [BindProperty]
    public string month { get; set; }
    [BindProperty]
    public string monthTo { get; set; }    
    [BindProperty]
    public string Password { get; set; }

    public IActionResult OnPost()
    {
        try
        {
            // Process the input value
            var password = this.Password;

            //string htmlContent = "<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"UTF-8\"><title>Association Fee Invoice - March 2025</title><style>body{font-family:Arial,sans-serif;margin:0;padding:20px}.invoice-container{max-width:800px;margin:0 auto;border:1px solid #ccc;padding:20px;background-color:#f9f9f9}h1{text-align:center;color:#333}.invoice-details,.items-table{width:100%;margin-bottom:20px}.invoice-details td{padding:10px;border:1px solid #ccc}.items-table td,.items-table th{padding:10px;text-align:center;border:1px solid #ccc}.total{text-align:right;font-size:18px;font-weight:700}</style></head><body><div class=\"invoice-container\"><h1>Association Fee Invoice</h1><table class=\"invoice-details\"><tr><td><strong>Name</strong></td><td>#name#</td></tr><tr><td><strong>House No.</strong></td><td>#house-no#</td></tr><tr><td><strong>Month</strong></td><td>#month#</td></tr><tr><td><strong>Amount</strong></td><td>#amount#</td></tr><tr><td><strong>Paid Date</strong></td><td>#paid-date#</td></tr></table><div>The Association Committee sincerely thanks you for your unwavering support in the development of <b>Yogeshwar Nagar Society</b>.</div><div style=\"padding-top:30px\"><div>Thanks & Regards,</div><div style=\"padding-top:5px\">Yogeshwar Nagar Committee</div></div></div></body></html>";
            string htmlContent = System.IO.File.ReadAllText("./wwwroot/Sample.html");

            if (password == "ygsadmin")
            {
                if (houseNo != null)
                {
                    var row = dtMemberList.Select($"[House no.] = '{houseNo}'");

                    if (row.Length > 0 && row[0]["Name"] != null || memberName != null)
                    {
                        int monthDiff = MonthDifference(month, monthTo);
                        string monthRange = (month.Equals(monthTo, StringComparison.InvariantCultureIgnoreCase)) ? $"{month}, {DateTime.Now.ToString("yyyy")}" :
                            $"{month}, {DateTime.Now.ToString("yyyy")}  -  {monthTo}, {DateTime.Now.ToString("yyyy")}";

                        var name = memberName ?? row[0]["Name"].ToString();
                        htmlContent = htmlContent.Replace("#name#", name);
                        htmlContent = htmlContent.Replace("#house-no#", houseNo);
                        htmlContent = htmlContent.Replace("#month#", monthRange);
                        htmlContent = htmlContent.Replace("#paid-date#", paidDate.ToString("dd-MMM-yyyy"));
                        htmlContent = htmlContent.Replace("#amount#", (associationFee * monthDiff).ToString());

                        // Create the HTML-to-PDF converter
                        //var converter = new BasicConverter(new PdfTools());

                        //string base64String = HtmlToBase64(htmlContent);
                        //var pdfBytes = System.Convert.FromBase64String(base64String);


                        using (MemoryStream pdfStream = new MemoryStream())
                        {
                            using (MemoryStream htmlStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(htmlContent)))
                            {
                                HtmlConverter.ConvertToPdf(htmlStream, pdfStream);
                            }

                            // Return the PDF file for download
                            // Return the PDF file directly to the client for download
                            var fileName = $"{houseNo}_{month}_receipt.pdf";
                            return File(pdfStream.ToArray(), "application/pdf", fileName);
                        }
                    }
                    else
                    {
                        TempData["Error"] = "Name not found in database.";
                        return RedirectToAction("Index", "Home");
                    }
                }
                else
                {
                    TempData["Error"] = "Please enter house number.";
                    return RedirectToAction("Index", "Home");
                }
            }
            else
            {
                TempData["Error"] = "Secret key field is empty or invalid. Please enter the valid secret key.";
                return RedirectToAction("Index", "Home");
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message + "############" + ex.StackTrace;
            return RedirectToAction("Index", "Home");
        }
    }

    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
    }

    static int MonthDifference(string month1, string month2)
    {
        // Convert month names to numbers using DateTimeFormatInfo
        int m1 = DateTime.ParseExact(month1, "MMMM", CultureInfo.InvariantCulture).Month;
        int m2 = DateTime.ParseExact(month2, "MMMM", CultureInfo.InvariantCulture).Month;

        // Compute absolute difference
        return Math.Abs(m1 - m2) + 1;
    }

    public async Task OnGet()
    {
        dtMemberList = await GetMemberdataFromSheet();
        TempData["defaultDateTime"] = DateTime.Now.ToString("yyyy-MM-dd");
        this.month = DateTime.Now.ToString("MMMM");
        this.monthTo = DateTime.Now.ToString("MMMM");
    }

    async Task<DataTable> GetMemberdataFromSheet()
    {
        string[] Scopes = { SheetsService.Scope.Spreadsheets };
        string ApplicationName = "YGS25";
        string SpreadsheetId = "1BJXP2l3__uc0F0AwO65CPSlQb0CqX_b1aF9DV_Ue-rY";  // Replace with your Google Sheet ID
        string SheetName = "Members"; // Replace with your sheet name
        string range = $"{SheetName}!A1:C73"; // Define range (adjust as needed)

        // Load credentials from the JSON key file
        try
        {
            string apiKey = "AIzaSyDR-Rm561qIXWpJwcNndKpUI1zEF13dfkw";
            var service = new SheetsService(new BaseClientService.Initializer()
            {
                ApiKey = apiKey,
                ApplicationName = "client1"
            });

            var request = service.Spreadsheets.Values.Get(SpreadsheetId, range);
            ValueRange response = await request.ExecuteAsync();
            IList<IList<object>> values = response.Values;

            return ConvertToDataTable(values);

        }
        catch (Exception e)
        {
            throw e;
        }
    }
    static DataTable ConvertToDataTable(IList<IList<object>> list)
    {
        DataTable table = new DataTable();

        if (list.Count == 0)
            return table;

        // Assume first row contains column headers
        for (int i = 0; i < list[0].Count; i++)
        {
            table.Columns.Add(list[0][i]?.ToString() ?? $"Column{i}");
        }

        // Add remaining rows as data
        for (int i = 1; i < list.Count; i++)
        {
            table.Rows.Add(list[i].ToArray());
        }

        return table;
    }
}
