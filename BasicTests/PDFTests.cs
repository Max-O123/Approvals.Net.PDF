using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;
using ApprovalTests;
using ApprovalTests.Reporters;
using ApprovalUtilities.Utilities;
using Ecark.ApprovalTests.PDF;


namespace BasicTests
{
    [TestClass]
    [UseReporter(typeof(PDFDiffLauncher))]
    public class PDFTests
    {


        [TestMethod]
        public void TestFilesThatShouldBeSame()
        {
            string filepath = PathUtilities.GetAdjacentFile("example.pdf");
            var bytes = File.ReadAllBytes(filepath);
            PDFApprovals.VerifyPDF(bytes);

        }
        [TestMethod]
        public void TestFilesThatShouldBeDifferent()
        {
            string filepath = PathUtilities.GetAdjacentFile("example2.pdf");
            var bytes = File.ReadAllBytes(filepath);
            PDFApprovals.VerifyPDF(bytes);

        }

    }
}