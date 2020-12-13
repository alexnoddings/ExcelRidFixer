using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ExcelRidFixer
{
    public class Program
    {
        private const string RegexPattern = @"(o:relid=""rId([0-9]+)"")(\s|\1)*o:relid=""rId([0-9]+)""";
        private const string RegexReplacement = @"o:relid=""rId$4""";
        
        private readonly string _inputFileName;
        private readonly bool _shouldBackup;

        public static async Task Main(string[] args)
        {
            if (args.Length < 1)
                throw new ArgumentException("Must provide an input file name argument");

            string inputFileName = args[0];
            bool shouldBackup = !args.Any(a => a == "nobackup");

            var program = new Program(inputFileName, shouldBackup);
            await program.RunAsync().ConfigureAwait(false);
        }

        public Program(string inputFileName, bool shouldBackup)
        {
            _inputFileName = inputFileName;
            _shouldBackup = shouldBackup;
        }

        public async Task RunAsync()
        {
            if (_shouldBackup)
                MakeBackup(_inputFileName);

            using var fileStream = new FileStream(_inputFileName, FileMode.Open);
            using var archive = new ZipArchive(fileStream, ZipArchiveMode.Update);

            var vmlDrawingFile = archive.GetEntry("xl/drawings/vmlDrawing1.vml");
            var contents = await GetEntryContentsAsync(vmlDrawingFile);
            var newContents = Regex.Replace(contents, RegexPattern, RegexReplacement, RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromSeconds(5));
            await WriteEntryContentsAsync(vmlDrawingFile, newContents);
        }

        private static void MakeBackup(string fileName) =>
            File.Copy(fileName, fileName + ".bak", true);

        private static async Task<string> GetEntryContentsAsync(ZipArchiveEntry entry)
        {
            using var fileReader = new StreamReader(entry.Open());
            return await fileReader.ReadToEndAsync().ConfigureAwait(false);
        }

        private static async Task WriteEntryContentsAsync(ZipArchiveEntry entry, string newContents)
        {
            using var fileWriter = new StreamWriter(entry.Open());
            await fileWriter.WriteAsync(newContents).ConfigureAwait(false);
        }
    }
}
