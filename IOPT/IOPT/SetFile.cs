/// checked 14-Jun-2018
using System;
using System.Diagnostics;
using System.IO;

namespace IOPT
{
    public class SetFile
    {
        /// <summary>
        /// open and clean a new file 
        /// </summary>
        /// <param name="FileName"></param>
        /// <returns></returns>
        protected internal static void CleanFile(string FileName)
        {
            FileStream file = new FileStream(FileName, FileMode.Create, FileAccess.Write);
            file.SetLength(0);
            file.Close();
        }

        /// <summary>
        /// Clean output txt files
        /// </summary>
        /// <returns></returns>
        protected internal static void CleanOutputFiles()
        {
            string FileName;
            FileName = MyFileNames.OutPutFolder+ "EventPath.txt"; CleanFile(FileName);
            FileName = MyFileNames.OutPutFolder+ "GlobalIter.txt"; CleanFile(FileName);
            FileName = MyFileNames.OutPutFolder+ "BB_Best_SolNum.txt"; CleanFile(FileName);
            FileName = MyFileNames.OutPutFolder+ "Bcm_PathIter.txt"; CleanFile(FileName);
            FileName = MyFileNames.OutPutFolder+ "Bcm_Seg.txt"; CleanFile(FileName);
            FileName = MyFileNames.OutPutFolder+ "Event_All.txt"; CleanFile(FileName);
            FileName = MyFileNames.OutPutFolder+ "Lines.txt"; CleanFile(FileName);
            FileName = MyFileNames.OutPutFolder+ "InputSchedule.txt"; CleanFile(FileName);
            FileName = MyFileNames.OutPutFolder+ "Trips.txt"; CleanFile(FileName);
            FileName = MyFileNames.OutPutFolder+ "Nodes.txt"; CleanFile(FileName);
            FileName = MyFileNames.OutPutFolder+ "Segs.txt"; CleanFile(FileName);
            FileName = MyFileNames.OutPutFolder+ "BB_Iter.txt"; CleanFile(FileName);
            FileName = MyFileNames.OutPutFolder+ "BB_Sol.txt"; CleanFile(FileName);
            FileName = MyFileNames.OutPutFolder+ "BB_LP_Path.txt"; CleanFile(FileName);
            FileName = MyFileNames.OutPutFolder+ "BB_LP_Fre.txt"; CleanFile(FileName);
            FileName = MyFileNames.OutPutFolder+ "BB_LP_Sch.txt"; CleanFile(FileName);
            FileName = MyFileNames.OutPutFolder+ "BB_LP_SchAtTerminal.txt"; CleanFile(FileName);
            FileName = MyFileNames.OutPutFolder+ "BB_LP_PasPath.txt"; CleanFile(FileName);
            FileName = MyFileNames.OutPutFolder+ "BB_LP_PasPath_Data.txt"; CleanFile(FileName);
            FileName = MyFileNames.OutPutFolder+ "GenEvtLog.txt"; CleanFile(FileName);
            FileName = MyFileNames.OutPutFolder+ "BB_ActivePathSet.txt"; CleanFile(FileName);
        }


        /// <summary>
        /// Create and set output folders
        /// read adjust para in the py folder
        /// </summary>
        /// <returns></returns>
        protected internal static void Init()
        {
            // read test case output folder
            string AssignType = "";
            using (StreamReader reader = new StreamReader(MyFileNames.AdjustPara))
            {
                char[] delimiters = new char[] { ',' };
                while (true)
                {
                    string line = reader.ReadLine();
                    if (line == null) break;
                    string[] parts = line.Split(delimiters);
                    if (parts[0].Equals("Assign"))
                    {
                        if (parts[1].Equals("SUE")) AssignType = "SUE";
                        if (parts[1].Equals("RSUE")) AssignType = "RSUE";
                        if (parts[1].Equals("BCM")) AssignType = "BCM";
                    }
                    if (parts[0].Equals("Case")) Global.TestCase = parts[1];
                    if (parts[0].Equals("TestIndex")) Global.TestCaseIndex = parts[1];
                }
            }
            Console.WriteLine("Adjust Para file has been read successfully");
            MyFileNames.InputFolder = MyFileNames.InputFolder + Global.TestCase+ Path.DirectorySeparatorChar;
            Trace.Assert(Directory.Exists(MyFileNames.InputFolder), "Input Folder is not set");

            MyFileNames.OutPutFolder = MyFileNames.OutPutFolder + Global.TestCase + Path.DirectorySeparatorChar + AssignType
                + '_' + Global.TestCaseIndex + Path.DirectorySeparatorChar ;
            if (!Directory.Exists(MyFileNames.OutPutFolder)) Directory.CreateDirectory(MyFileNames.OutPutFolder);
            MyFileNames.LogFolder = MyFileNames.LogFolder +  Global.TestCase + Path.DirectorySeparatorChar;
            if (!Directory.Exists(MyFileNames.LogFolder)) Directory.CreateDirectory(MyFileNames.LogFolder);
            string sourcePath = MyFileNames.InputFolder;
            string targetPath = MyFileNames.OutPutFolder +"InPut" + Path.DirectorySeparatorChar;
            if (!Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(targetPath);
                Console.WriteLine(targetPath + "has been created");
            }
            // Copy input files and overwrite destination files if they already exist.
            string[] files = Directory.GetFiles(sourcePath);
            foreach (string s in files)
            {
                string fileName = Path.GetFileName(s);
                string destFile = Path.Combine(targetPath, fileName);
                File.Copy(s, destFile, true);
            }
            CleanOutputFiles();
            MyLog.Instance.Info("SetFileIni is completed");
        }

    }
}
