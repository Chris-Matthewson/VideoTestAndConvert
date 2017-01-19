using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace VideoTester.BackgroundWorkers
{
    public class VideoConverterBackgroundWorker
    {
        public event EventHandler<int> ProgressChanged;
        public event EventHandler<string> Complete;

        public bool IsBusy => _worker.IsBusy;
        public string CurrentPath = "";

        private readonly BackgroundWorker _worker = new BackgroundWorker
        {
            WorkerReportsProgress = true,
            WorkerSupportsCancellation = true
        };
        public VideoConverterBackgroundWorker()
        {
            _worker.DoWork += WorkerOnDoWork;
            _worker.ProgressChanged += WorkerOnProgressChanged;
            _worker.RunWorkerCompleted += WorkerOnRunWorkerCompleted;
        }

        public void CancelCurrentJob()
        {
            _worker.CancelAsync();
        }

        public void StartWorker(string inFile, string outFile)
        {
            CurrentPath = inFile;
            _worker.RunWorkerAsync(new []{inFile, outFile});
        }

        private void WorkerOnDoWork(object sender, DoWorkEventArgs doWorkEventArgs)
        {
            try
            {
                var files = doWorkEventArgs.Argument as string[];
                if (files == null || files.Length != 2)
                {
                    return;
                }

                var infile = files[0];
                var outfile = files[1];
                var total = GetVideoDuration(infile);

                if (Path.GetDirectoryName(outfile) != null && !Directory.Exists(Path.GetDirectoryName(outfile)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(outfile));
                }

                var cmd = " -i \"" + infile + "\" -pix_fmt yuv420p \"" + outfile + "\"";

                var startInfo = new ProcessStartInfo
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "ffmpeg.exe",
                    Arguments = cmd + " -y",
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                var process = new Process
                {
                    StartInfo = startInfo
                };

                process.Start();

                var reader = process.StandardError;
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("frame= "))
                    {
                        var remaining = line.Split(' ');
                        Debug.WriteLine(line);
                        foreach (var s in remaining)
                        {
                            if (s.StartsWith("time="))
                            {
                                var current = TimeSpan.Parse(s.Replace("time=", "")).TotalSeconds;
                                
                                Debug.WriteLine((int)(current / total * 100.00));
                                ((BackgroundWorker)sender).ReportProgress((int)(current / total * 100.00));
                                if (((BackgroundWorker) sender).CancellationPending)
                                {
                                    process.Kill();
                                    Task.Run(() =>
                                    {
                                        Thread.Sleep(1000);
                                        try
                                        {
                                            File.Delete(outfile);
                                        }
                                        catch (Exception)
                                        {
                                            Thread.Sleep(2000);
                                            File.Delete(outfile);
                                        }
                                    });
                                    doWorkEventArgs.Result = "";
                                    return;
                                }
                            }
                        }
                    }
                }

                doWorkEventArgs.Result = infile;
            }
            catch (Exception ex)
            {
                doWorkEventArgs.Result = "";
                Debug.WriteLine(ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        private void WorkerOnProgressChanged(object sender, ProgressChangedEventArgs progressChangedEventArgs)
        {
            ProgressChanged?.Invoke(this, progressChangedEventArgs.ProgressPercentage);
        }

        private void WorkerOnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs runWorkerCompletedEventArgs)
        {
            CurrentPath = "";
            Complete?.Invoke(this, (string)runWorkerCompletedEventArgs.Result);
        }

        /// <summary>
        ///     Gets the duration of a video file using magic and trickery.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static double GetVideoDuration(string path)
        {
            var cmd = "ffprobe -v error -select_streams v:0 -show_entries stream=duration -of default=noprint_wrappers=1:nokey=1 \"" + path + "\"";

            var startInfo = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "cmd.exe",
                Arguments = "/c " + cmd,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var process = new Process
            {
                StartInfo = startInfo
            };

            process.Start();
            var reader = process.StandardOutput;
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                Debug.WriteLine(line);
                return double.Parse(line);
            }
            return 0;
        }

    }
}
