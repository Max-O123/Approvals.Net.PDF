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

        public virtual bool Approve()
        {
            if (this.path == null)
            {
                string basename = Path.Combine(this.namer.SourcePath, this.namer.Name);
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
            switch (tag)
            {
                // in theory this is all the metadata tags, should ignore these in comparison
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
                case "iText-Producer":
                case "iText-Version":

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