using System.Collections.Generic;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Console.Domain;
using Console.Repo;
using Console.UI;
using Console.Utilities;

namespace Console.Commands
{
    public class Kronos : ICommand
    {
        public async Task Run()
        {
            UIConsole.Show("Creating Kronos sheet... \n");

            var regions = await RepoRegionDump.Dump.Regions();
            await Sheet(regions);

            UIConsole.Show("Done. \n");
        }

        private async Task Sheet(List<Region> regions)
        {
            var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("TimeSheet");

            var row = new List<object>
            {
                "Region", "Major", "Minor", "Nations", "Endo's", "Protected", "Exec. D", "Tagged", "Link", "", "World",
                "Data"
            };
            ws.AddRow(1, row);

            var majorTime = await RepoRegionDump.Dump.MajorTook();
            var minorTime = await RepoRegionDump.Dump.MinorTook();
            var nations = await RepoRegionDump.Dump.NumNations();

            ws.AddWorldData(2, 11, nations, (int) majorTime, (int) minorTime);

            for (var i = 2; i < regions.Count + 2; i++)
            {
                var region = regions[i - 2];
                row = new List<object>
                {
                    "'" + region.name,
                    "'" + region.readableMajorUpdateTime,
                    "'" + region.readableMinorUpdateTime,
                    region.nationCount,
                    region.delegateVotes,
                    !region.founderless ? "Founder" : region.password ? "Password" : "No",
                    region.delegateAuthority.ToUpper().Contains("X") ? "Y" : "N",
                    region.tagged ? "Y" : "N"
                };
                ws.AddRow(i, row);
                ws.Cell($"I{i}").SetValue(region.url).Hyperlink = new XLHyperlink(region.url);
            }

            ws.Range("A1:I1").Style.Fill.BackgroundColor = XLColor.Gray;
            ws.Range("K1:L1").Style.Fill.BackgroundColor = XLColor.Gray;
            ws.Row(1).Style.Font.Bold = true;

            ws.Columns(1, 12).AdjustToContents();
            ws.Column("I").Width = 40;
            ws.Range("I2", $"G{regions.Count + 1}").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Justify;
            ws.Column("K").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            ws.Column("L").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

            ws.Range("F2", $"F{regions.Count + 1}").AddConditionalFormat().WhenStartsWith("F").Fill
                .SetBackgroundColor(XLColor.Green);
            ws.Range("F2", $"F{regions.Count + 1}").AddConditionalFormat().WhenStartsWith("P").Fill
                .SetBackgroundColor(XLColor.Olive);
            ws.Range("F2", $"F{regions.Count + 1}").AddConditionalFormat().WhenStartsWith("N").Fill
                .SetBackgroundColor(XLColor.Yellow);
            ws.Range("G2", $"G{regions.Count + 1}").AddConditionalFormat().WhenEndsWith("Y").Fill
                .SetBackgroundColor(XLColor.DarkOrange);
            ws.Range("G2", $"G{regions.Count + 1}").AddConditionalFormat().WhenEndsWith("N").Fill
                .SetBackgroundColor(XLColor.Green);
            ws.Range("H2", $"H{regions.Count + 1}").AddConditionalFormat().WhenEndsWith("Y").Fill
                .SetBackgroundColor(XLColor.Red);
            ws.Range("H2", $"H{regions.Count + 1}").AddConditionalFormat().WhenEndsWith("N").Fill
                .SetBackgroundColor(XLColor.Green);

            wb.SaveAs($"Kronos-TimeSheet_{TimeUtil.DateForPath()}.xlsx");
        }
    }
}