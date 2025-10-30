using ApprovalTests.Core;
using ApprovalTests.Core.Exceptions;
using ApprovalUtilities.SimpleLogger;

namespace Ecark.ApprovalTests.PDF
{
    public class PDFApprover : IApprovalApprover
    {
        private readonly IApprovalNamer namer;
        private readonly bool deleteOnSuccess;
        private readonly IApprovalWriter writer;
        private string approved;
        private string received;
        private ApprovalException failure;
        private string path; //optional feild to use if storing test files outside of the directory containing the tests

        public PDFApprover(IApprovalWriter writer, IApprovalNamer namer, bool deleteOnSuccess, string path = null)
        {
            this.writer = writer;
            this.namer = namer;
            this.deleteOnSuccess = deleteOnSuccess;
            this.path = path;
        }
        private string getTestName(string input)
        {
            string name = "";
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == '.')
                {
                    break;
                }
                name += input[i];
            }
            return name;
        }
        public virtual bool Approve()
        {
            if (this.path == null)
            {
                string testName = getTestName(this.namer.Name);
                string folderGen = Path.Combine(this.namer.SourcePath, $"{testName}ApprovalTestOutput");
                System.IO.Directory.CreateDirectory(folderGen);
                string basename = Path.Combine(folderGen, this.namer.Name);
                this.approved = Path.GetFullPath(this.writer.GetApprovalFilename(basename));
                this.received = Path.GetFullPath(this.writer.GetReceivedFilename(basename));
                this.received = this.writer.WriteReceivedFile(this.received);
            }
            else
            {
                string basename = Path.Combine(this.path, this.namer.Name);
                this.approved = Path.GetFullPath(this.writer.GetApprovalFilename(basename));
                this.received = Path.GetFullPath(this.writer.GetReceivedFilename(basename));
                this.received = this.writer.WriteReceivedFile(this.received);
            }


            this.failure = this.Approve(this.approved, this.received);
            return this.failure == null;
        }

        public virtual ApprovalException Approve(string approvedPath, string receivedPath)
        {
            if (!File.Exists(approvedPath))
            {
                return new ApprovalMissingException(receivedPath, approvedPath);
            }

            return !Compare(receivedPath, approvedPath)
                ? new ApprovalMismatchException(receivedPath, approvedPath)
                : null;
        }

        private bool Compare(string receivedPath, string approvedPath)
        {

            var approvedExtractor = new global::Ecark.ApprovalTests.PDF.PdfObjectExtractor();
            approvedExtractor.Extract(approvedPath);

            var receivedExtractor = new global::Ecark.ApprovalTests.PDF.PdfObjectExtractor();
            receivedExtractor.Extract(receivedPath);

            var approvedDict = approvedExtractor.BytesByKey;
            var receivedDict = receivedExtractor.BytesByKey;

            foreach (var kv in approvedDict)
            {
                string tag = kv.Key;
                var approvedByteLists = kv.Value;

                if (!receivedDict.TryGetValue(tag, out var receivedByteLists))
                {
                    Logger.Event($"Missing tag in received PDF: {tag}");
                    return false;
                }

                if (approvedByteLists.Count != receivedByteLists.Count)
                {
                    Logger.Event($"Different number of entries for tag '{tag}': Approved={approvedByteLists.Count}, Received={receivedByteLists.Count}");
                    return false;
                }

                for (int i = 0; i < approvedByteLists.Count; i++)
                {
                    byte[] approvedBytes = approvedByteLists[i];
                    byte[] receivedBytes = receivedByteLists[i];

                    if (!AreEqual(tag, approvedBytes, receivedBytes))
                    {
                        Logger.Event($"Failed on tag '{tag}'[{i}]");
                        return false;
                    }
                }
            }
            foreach (var kv in receivedDict)
            {
                string tag = kv.Key;
                if (!approvedDict.ContainsKey(tag))
                {
                    Logger.Event($"Unexpected tag in received PDF: {tag}");
                    return false;
                }
            }

            return true;
        }



        private bool AreEqual(string tag, byte[] approved, byte[] received)
        {
            if (isProblematicTag(tag))
            {
                return true;
            }
            if (approved.Length != received.Length)
                return false;
            if (tag == "FontName" || tag == "BaseFont")
            {
                string temp1 = "";
                string temp2 = "";
                temp1 = approved.ToString().Substring(7);
                temp2 = received.ToString().Substring(7);

                if (temp1 == temp2)
                {
                    return true;
                }
                else
                {
                    Logger.Event($"Failed on tag: {tag}, values were: {temp1} and {temp2}");
                    return false;
                }

            }
            for (int i = 0; i < approved.Length; i++)
            {
                if (approved[i] != received[i])
                {
                    
                    Logger.Event("Failed on {0}[{1}]      '{2}' != '{3}'", tag, i,
                        (char)received[i], (char)approved[i]);
                    return false;
                }
            }

            return true;
        }

        private bool isProblematicTag(string tag)
        {
            if (tag.StartsWith("F") && tag.Length > 1 && char.IsDigit(tag[1]))
            {
                return true;
            }
            switch (tag)
            {
                // These tags are all nondeterministic and have the habit of changing upon generation so we need to ignore them
                case "CreationDate":
                case "ModDate":
                case "Producer":
                case "Creator":
                case "Title":
                case "Subject":
                case "Keywords":
                case "Author":
                case "ID":                 
                case "Metadata":          
                case "PieceInfo":         
                case "DocChecksum":       
                case "LastModified":
                case "Trapped":
                case "MarkInfo":
                case "StructTreeRoot":
                case "xmp:CreateDate":
                case "xmp:ModifyDate":
                case "xmp:MetadataDate":
                case "xmpMM:DocumentID":
                case "xmpMM:InstanceID":
                case "xmpTPg:NPages": 
                case "iText":
                case "FontDescriptor":
                case "DescendantFonts":
                case "iText-Producer":
                case "iText-Version":
                case "UUID":                    // Generic UUIDs that might appear in custom metadata
                case "GUID":                    // Sometimes used instead of UUID
                case "DocumentID":              // Sometimes outside xmpMM namespace
                case "InstanceID":              // Same as above
                case "ProducerVersion":         
                case "CreationTime":            // Sometimes alternative timestamp key
                case "ModTime":                 // Alternative modification time
                case "EmbeddedFileChecksum":    // Embedded file checksum metadata
                case "EmbeddedFileModDate":     // Modification date for embedded files
                case "BaseFontPrefix":          
                    return true;

                default:
                    return false;
            }
        }

        public void Fail()
        {
            throw this.failure;
        }

        public void ReportFailure(IApprovalFailureReporter reporter)
        {
            reporter.Report(this.approved, this.received);
        }

        public void CleanUpAfterSuccess(IApprovalFailureReporter reporter)
        {
            if (deleteOnSuccess)
            {
                File.Delete(this.received);
            }

            doCleanUp(reporter);
        }

        public void doCleanUp(IApprovalFailureReporter reporter)
        {
            var withCleanUp = reporter as IApprovalReporterWithCleanUp;
            if (withCleanUp != null)
            {
                withCleanUp.CleanUp(this.approved, this.received);
            }
        }
    }
}