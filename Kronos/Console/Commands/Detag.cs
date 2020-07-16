using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Console.Domain;
using Console.Repo;
using Console.UI;
using Console.Utilities;

namespace Console.Commands
{
    public class Detag : ICommand
    {
        public async Task Run()
        {
            UIConsole.Show("Creating Detag sheet... \n");

            var regions = await RepoRegionDump.Dump.Regions();
            regions = Filter(regions);
            await Sheet(regions);

            UIConsole.Show("Done. \n");
        }

        private List<Region> Filter(List<Region> regions)
        {
            return regions.FindAll(r => !r.password && r.delegateAuthority.ToUpper().Contains("X") && r.tagged)
                .ToList();
        }

        private async Task Sheet(List<Region> regions)
        {
            var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("TimeSheet");

            var row = new List<object>
                {"Region", "Major", "Minor", "Nations", "Endo's", "Founder", "Link", "", "World", "Data"};
            ws.AddRow(1, row);

            var majorTime = await RepoRegionDump.Dump.MajorTook();
            var minorTime = await RepoRegionDump.Dump.MinorTook();
            var nations = await RepoRegionDump.Dump.NumNations();

            ws.AddWorldData(2, 9, nations, (int) majorTime, (int) minorTime);

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
                    !region.founderless ? "Y" : "N"
                };
                ws.AddRow(i, row);
                ws.Cell($"G{i}").SetValue(region.url).Hyperlink = new XLHyperlink(region.url);
            }

            ws.Range("A1:G1").Style.Fill.BackgroundColor = XLColor.Gray;
            ws.Range("I1:J1").Style.Fill.BackgroundColor = XLColor.Gray;
            ws.Row(1).Style.Font.Bold = true;

            ws.Columns(1, 9).AdjustToContents();
            ws.Column("G").Width = 40;
            ws.Range("G2", $"G{regions.Count + 1}").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Justify;
            ws.Column("I").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            ws.Column("J").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

            ws.Range("F2", $"F{regions.Count + 1}").AddConditionalFormat().WhenEndsWith("Y").Fill
                .SetBackgroundColor(XLColor.Green);
            ws.Range("F2", $"F{regions.Count + 1}").AddConditionalFormat().WhenEndsWith("N").Fill
                .SetBackgroundColor(XLColor.Red);

            wb.SaveAs($"Kronos-Detag_{TimeUtil.DateForPath()}.xlsx");
        }
    }
}