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
    public class Kronos : ICommand
    {
        private static List<Region> regionsParsed;

        public async Task Run()
        {
            UIConsole.Show("Creating Kronos sheet... \n");
            
            var regions = await Regions();
            Sheet(regions);

            UIConsole.Show("Done. \n");
        }

        public static async Task<List<Region>> Regions()
        {
            if (regionsParsed != null) return regionsParsed;

            var api = RepoApi.Api;
            var dump = RepoDump.Dump;

            var startOfLastMajor = TimeUtil.PosixLastMajorStart();
            var endOfLastMajor = await dump.EndOfMajor();
            var majorDuration = endOfLastMajor - startOfLastMajor;
            var majorTick = majorDuration / await api.NumNations();

            var startOfLastMinor = TimeUtil.PosixLastMinorStart();
            var endOfLastMinor = await api.EndOfMinor();
            var minorDuration = endOfLastMinor - startOfLastMinor;
            var minorTick = minorDuration / await api.NumNations();

            var regions = await dump.Regions();
            regions = AddMinorUpdateTimes(regions, minorTick);
            regions = AddReadableUpdateTimes(regions);
            regionsParsed = regions;

            return regions;
        }

        public static List<Region> AddMinorUpdateTimes(List<Region> regions, double minorTick)
        {
            for (var i = 0; i < regions.Count; i++)
                regions[i].minorUpdateTime = i == 0
                    ? regions[i].nationCount * minorTick + TimeUtil.PosixLastMinorStart()
                    : regions[i - 1].minorUpdateTime + regions[i].nationCount * minorTick;

            return regions;
        }

        public static List<Region> AddReadableUpdateTimes(List<Region> regions)
        {
            for (var i = 0; i < regions.Count; i++)
            {
                regions[i].readableMajorUpdateTime = TimeUtil.ToUpdateOffset(regions[i].majorUpdateTime);
                regions[i].readableMinorUpdateTime = TimeUtil.ToUpdateOffset(regions[i].minorUpdateTime);
            }

            return regions;
        }
        
        private void Sheet(List<Region> regions)
        {
            var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("TimeSheet");

            var row = new List<object>
                {"Region", "Major", "Minor", "Nations", "Endo's", "Protected", "Exec. D", "Tagged", "Link", "", "World", "Data"};
            ws.AddRow(1, row);

            var majorTime = regions.Last().majorUpdateTime - regions.First().majorUpdateTime;
            var minorTime = regions.Last().minorUpdateTime - regions.First().minorUpdateTime;
            var nations = RepoDump.Dump.NumNations;

            ws.AddWorldData(2, 11, nations, (int) majorTime, (int) minorTime);

            for (var i = 2; i < regions.Count + 2; i++)
            {
                var region = regions[i - 2];
                row = new List<object>
                {
                    region.name,
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