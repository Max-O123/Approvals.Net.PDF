using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;
using ApprovalTests;
using ApprovalTests.Reporters;
using ApprovalUtilities.Utilities;



namespace BasicTests
{
    [TestClass]
    [UseReporter(typeof(ApprovalTests.PDF.PDFDiffLauncher))]
    public class PDFTests
    {


        [TestMethod]
        public void TestFilesThatShouldBeSame()
        {
            string filepath = PathUtilities.GetAdjacentFile("example.pdf");
            var bytes = File.ReadAllBytes(filepath);
            ApprovalTests.PDF.PDFApprovals.VerifyPDF(bytes);

        }
        [TestMethod]
        public void TestFilesThatShouldBeDifferent()
        {
            string filepath = PathUtilities.GetAdjacentFile("example2.pdf");
            var bytes = File.ReadAllBytes(filepath);
            ApprovalTests.PDF.PDFApprovals.VerifyPDF(bytes);

        }

    }
}