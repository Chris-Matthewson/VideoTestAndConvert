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
        public event EventHandler<object[]> ProgressChanged;
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
            var files = doWorkEventArgs.Argument as string[];
            if (files == null || files.Length != 2)
            {
                return;
            }

            var infile = files[0];
            var outfile = files[1];
            try
            {
                
                var total = GetVideoDuration(infile);

                if (Path.GetDirectoryName(outfile) != null && !Directory.Exists(Path.GetDirectoryName(outfile)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(outfile));
                }

                string ffmpegCommand;

                if (Path.GetExtension(infile)?.ToLower() == ".mkv")
                {
                    //ffmpegCommand = "-i  \"" + infile + "\"  -c:v libx264 -c:a aac -b:a 128k -pix_fmt yuv420p  \"" + outfile + "\"";
                    ffmpegCommand = "-i  \"" + infile + "\"  -vcodec copy -acodec copy -pix_fmt yuv420p  \"" + outfile + "\"";
                    Debug.WriteLine(ffmpegCommand);
                }
                else
                {
                    ffmpegCommand = " -i \"" + infile + "\" -pix_fmt yuv420p \"" + outfile + "\"";
                }


                var startInfo = new ProcessStartInfo
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "ffmpeg.exe",
                    Arguments = ffmpegCommand + " -y",
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                var process = new Process
                {
                    StartInfo = startInfo
                };


                process.Start();
                var startTime = DateTime.Now;

                var reader = process.StandardError;
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    Debug.WriteLine(line);
                    if (line.StartsWith("frame="))
                    {
                        var remaining = line.Split(' ');
                        foreach (var s in remaining)
                        {
                            if (s.StartsWith("time="))
                            {
                                var current = TimeSpan.Parse(s.Replace("time=", "")).TotalSeconds;

                                var elapsedTime = (DateTime.Now - startTime).TotalSeconds;
                                var currentPercentage = current/total*100.00;

                                int estRemaining;
                                if (elapsedTime > 5)
                                {
                                    estRemaining = (int) (TimeSpan.FromSeconds((100.0 - currentPercentage)/(currentPercentage/elapsedTime)).TotalMinutes + 1);
                                    Debug.WriteLine(TimeSpan.FromSeconds((100.0 - currentPercentage) / (currentPercentage / elapsedTime)).TotalMinutes.ToString("0.00") + " Minutes remaining");
                                }
                                else
                                {
                                    estRemaining = 0;
                                }
                                //Debug.WriteLine((int)currentPercentage + " est:" + estRemaining.TotalSeconds);
                                ((BackgroundWorker)sender).ReportProgress((int)(current / total * 100.00), estRemaining.ToString("0"));
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
                doWorkEventArgs.Result = infile + ",FAILED";
                Debug.WriteLine(ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        private void WorkerOnProgressChanged(object sender, ProgressChangedEventArgs progressChangedEventArgs)
        {
            ProgressChanged?.Invoke(this, new object[]
            {
                progressChangedEventArgs.ProgressPercentage,
                progressChangedEventArgs.UserState as string
            });
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
            if (Path.GetExtension(path)?.ToLower() == ".mkv")
            {
                var cmd = "-i \"" + path + "\" - f null";

                var startInfo = new ProcessStartInfo
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "ffmpeg.exe",
                    Arguments = "/c " + cmd,
                    RedirectStandardOutput = true,
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
                    //Debug.WriteLine(line);
                    var remaining = line.Split(',');
                    foreach (var s in remaining)
                    {
                        if (s.Trim().StartsWith("Duration: "))
                        {
                            var duration = TimeSpan.Parse(s.Replace("Duration:", "")).TotalSeconds;

                            Debug.WriteLine(duration);
                            return duration;
                        }
                    }
                }
                return 0;
            }
            else
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
}
