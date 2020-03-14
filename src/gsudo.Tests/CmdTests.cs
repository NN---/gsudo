﻿using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using gsudo.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace gsudo.Tests
{
    [TestClass]
    public class CmdTests
    {
        static CmdTests()
        {
            // Start elevated service.
            var callingSid = WindowsIdentity.GetCurrent().User.Value;

            // start elevated service (to prevent uac popups).
            Process.Start($"gsudo", $@" gsudoservice 0 {callingSid} All");
        }

        [TestMethod]
        public void Cmd_DebugTests()
        {
            var p = new TestProcess("start cmd");
        }

        [TestMethod]
        public void Cmd_AdminUserTest()
        {
            Assert.IsTrue(ProcessHelper.IsAdministrator(), "This test suite is intended to be run as an administrator, otherwise it will not work.");
        }

        [TestMethod]
        public void Cmd_DirTest()
        {
            var p = new TestProcess("gsudo cmd /c dir");
            p.WaitForExit();
            Assert.AreEqual(string.Empty, p.GetStdErr());
            Assert.IsTrue(p.GetStdOut().Contains(" bytes free"));
            Assert.AreEqual(0, p.ExitCode);
        }

        [TestMethod]
        public void Cmd_ChangeDirTest()
        {

            // TODO: Test --raw, --vt, --attached
            var testDir = Environment.CurrentDirectory;
            var p1 = new TestProcess(
                             $"\"{testDir}\\gsudo\" cmd /c cd \r\n"
                                     + $"cd .. \r\n"
                                     + $"\"{testDir}\\gsudo\" cmd /c cd \r\n"
            );
            p1.WaitForExit();

            var otherDir = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory,".."));

            Assert.AreEqual(string.Empty, p1.GetStdErr());

            p1.GetStdOut()
                .AssertHasLine($"{testDir}")
                .AssertHasLine($"{otherDir}");

            Assert.AreEqual(0, p1.ExitCode);
        }
        [TestMethod]
        public void Cmd_EchoDoubleQuotesTest()
        {
            var p = new TestProcess("gsudo cmd /c echo 1 \"2 3\"");
            p.WaitForExit();
            p.GetStdOut().AssertHasLine("1 \"2 3\"");
            Assert.AreEqual(0, p.ExitCode);
        }

        [TestMethod]
        public void Cmd_EchoSimpleQuotesTest()
        {
            var p = new TestProcess("gsudo cmd /c echo 1 \'2 3\'");
            p.WaitForExit();
            p.GetStdOut().AssertHasLine("1 \'2 3\'");
            Assert.AreEqual(0, p.ExitCode);
        }

        [TestMethod]
        public void Cmd_ExitCodeTest()
        {
            var p = new TestProcess("gsudo exit 12345");
            p.WaitForExit();
            Assert.AreEqual(12345, p.ExitCode);
            
            p = new TestProcess("gsudo exit 0");
            p.WaitForExit();
            Assert.AreEqual(0, p.ExitCode);
        }

        [TestMethod]
        public void Cmd_CommandLineAppNoWaitTest()
        {
            // ping should take 20 seconds
            var p = new TestProcess("gsudo -n ping 127.0.0.1 -n 20"); 
            // but gsudo should exit immediately.
            p.WaitForExit(10000);
        }

        [TestMethod]
        public void Cmd_WindowsAppWaitTest()
        {
            bool stillWaiting = false;
            var p = new TestProcess("gsudo -w notepad");
            try
            {
                p.WaitForExit(2000);
            }
            catch (Exception)
            {
                stillWaiting = true;
            }

            Assert.IsTrue(stillWaiting);
            p.Kill();
        }

        [TestMethod]
        public void Cmd_WindowsAppNoWaitTest()
        {
            var p = new TestProcess("gsudo notepad");
            try
            {
                p.WaitForExit();
            }
            finally
            {
                p.Kill();
            }
        }

        [TestMethod]
        public void Cmd_WindowsAppWithQuotesTest()
        {
            var p = new TestProcess("gsudo \"c:\\Program Files (x86)\\Windows NT\\Accessories\\wordpad.exe\"");
            try
            {
                p.WaitForExit();
                Assert.AreEqual(0, p.ExitCode);
            }
            finally
            {
                Process.Start("taskkill.exe", "/IM Wordpad.exe").WaitForExit();
            }
        }

        [TestMethod]
        public void Cmd_UnexistentAppTest()
        {
            var p = new TestProcess("gsudo qaqswswdewfwerferfwe");
            p.WaitForExit();
            Assert.AreNotEqual(0, p.ExitCode);
        }

        [TestMethod]
        public void Cmd_BatchFileWithoutExtensionTest()
        {
            File.WriteAllText("HelloWorld.bat", "@echo Hello");

            var p = new TestProcess("gsudo HelloWorld");
            p.WaitForExit();
            Assert.AreEqual(string.Empty, p.GetStdErr());
            Assert.IsTrue(p.GetStdOut().Contains("Hello\r\n"));
            Assert.AreEqual(0, p.ExitCode);
        }
    }
}
