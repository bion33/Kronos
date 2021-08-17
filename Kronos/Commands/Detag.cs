using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Kronos.Domain;
using Kronos.Repo;
using Kronos.Utilities;

namespace Kronos.Commands
{
    /// <summary>
    ///     Command to generate a sheet with update times and information for detag-able regions.
    ///     Use the "Run" method to execute.
    /// </summary>
    public class Detag : ICommand
    {
        private RepoRegionDump dump;

        /// <summary> Generate a sheet with update times and information for detag-able regions </summary>
        public async Task Run(string userAgent, Dictionary<string, string> userTags, bool interactiveLog = false)
        {
            dump = RepoRegionDump.Dump(userAgent, userTags);
            var regions = await dump.Regions(interactiveLog);

            if (interactiveLog) Console.Write("Creating detag sheet... ");

            regions = Filter(regions);
            await XlsxSheet(regions);

            if (interactiveLog) Console.Write("[done].\n");
        }

        /// <summary>
        ///     Filter regions to return only those which have no password, executive WA Delegate authority, and are
        ///     tagged "invader".
        /// </summary>
        private static List<Region> Filter(List<Region> regions)
        {
            return regions.FindAll(r =>
            {
                return !r.Password && r.DelegateAuthority.ToUpper().Contains("X")
                                   && (r.Tagged || r.Embassies.Any(e => e.EmbassyType == EmbassyClass.RaiderRegions));
            }).ToList();
        }

        /// <summary> Generate a XLSX sheet containing the relevant information </summary>
        private async Task XlsxSheet(List<Region> regions)
        {
            var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("TimeSheet");

            // Header
            var row = new List<object>
                {"Region", "Major", "Minor", "Nations", "Endo's", "Founder", "Link", "", "World", "Data"};
            ws.AddRow(1, row);

            var majorTime = await dump.MajorTook();
            var minorTime = await dump.MinorTook();
            var nations = await dump.NumNations();

            // Add overall update information
            ws.AddWorldData(2, 9, nations, (int) majorTime, (int) minorTime);

            // Add for each region its name, major and minor update, nations, votes for its WA Delegate, whether
            // it has a founder or not, and its hyperlink.
            for (var i = 2; i < regions.Count + 2; i++)
            {
                var region = regions[i - 2];
                row = new List<object>
                {
                    "'" + region.Name,
                    "'" + region.ReadableMajorUpdateTime,
                    "'" + region.ReadableMinorUpdateTime,
                    region.NationCount,
                    region.DelegateVotes,
                    !region.Founderless ? "Y" : "N"
                };
                ws.AddRow(i, row);
                ws.Cell($"G{i}").SetValue(region.Url).Hyperlink = new XLHyperlink(region.Url);
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
            var date = TimeUtil.DateForPath();
            Directory.CreateDirectory(date);
            wb.SaveAs($"{date}/Kronos-Detag_{date}.xlsx");
        }
    }
}