using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Kronos.Domain;
using Kronos.Repo;
using Kronos.UI;
using Kronos.Utilities;

namespace Kronos.Commands
{
    /// <summary> Command to generate a sheet with update times and information for detag-able regions </summary>
    public class Detag : ICommand
    {
        /// <summary> Generate a sheet with update times and information for detag-able regions </summary>
        public async Task Run()
        {
            var regions = await RepoRegionDump.Dump.Regions();

            UIConsole.Show("Creating Detag sheet... ");

            regions = Filter(regions);
            await Sheet(regions);

            UIConsole.Show("[done].\n");
        }

        /// <summary>
        ///     Filter regions to return only those which have no password, executive WA Delegate authority, and are
        ///     tagged "invader".
        /// </summary>
        private List<Region> Filter(List<Region> regions)
        {
            return regions.FindAll(r => !r.password && r.delegateAuthority.ToUpper().Contains("X") && r.tagged)
                .ToList();
        }

        /// <summary> Generate a XLSX sheet containing the relevant information </summary>
        private async Task Sheet(List<Region> regions)
        {
            var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("TimeSheet");

            // Header
            var row = new List<object>
                {"Region", "Major", "Minor", "Nations", "Endo's", "Founder", "Link", "", "World", "Data"};
            ws.AddRow(1, row);

            var majorTime = await RepoRegionDump.Dump.MajorTook();
            var minorTime = await RepoRegionDump.Dump.MinorTook();
            var nations = await RepoRegionDump.Dump.NumNations();

            // Add overall update information
            ws.AddWorldData(2, 9, nations, (int) majorTime, (int) minorTime);

            // Add for each region its name, major and minor update, nations, votes for its WA Delegate, whether
            // it has a founder or not, and its hyperlink.
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

            // Style header
            ws.Range("A1:G1").Style.Fill.BackgroundColor = XLColor.Gray;
            ws.Range("I1:J1").Style.Fill.BackgroundColor = XLColor.Gray;
            ws.Row(1).Style.Font.Bold = true;

            // Align
            ws.Columns(1, 9).AdjustToContents();
            ws.Column("G").Width = 40;
            ws.Range("G2", $"G{regions.Count + 1}").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Justify;
            ws.Column("I").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            ws.Column("J").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

            // Add conditional colours for whether or not the region has a founder
            ws.Range("F2", $"F{regions.Count + 1}").AddConditionalFormat().WhenEndsWith("Y").Fill
                .SetBackgroundColor(XLColor.Green);
            ws.Range("F2", $"F{regions.Count + 1}").AddConditionalFormat().WhenEndsWith("N").Fill
                .SetBackgroundColor(XLColor.Red);

            // Save
            wb.SaveAs($"Kronos-Detag_{TimeUtil.DateForPath()}.xlsx");
        }
    }
}