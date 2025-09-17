using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FFMPEG_Converter
{
    public partial class Form1 : Form
    {
        private string saveFolderPath = "";
        public Form1()
        {
            InitializeComponent();
            comboBox1.SelectedIndex = 8;
            comboBox2.SelectedIndex = 0;
            comboBox3.SelectedIndex = 0;
            comboBox4.SelectedIndex = 0;
            comboBox5.SelectedIndex = 0;
            comboBox6.SelectedIndex = 0;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Media Files|*.mp4;*.mp3;*.avi;*.mov;*.wav;*.mkv;*.ts;*.*"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                foreach (string file in openFileDialog.FileNames)
                {
                    listBox1.Items.Add(file);
                }
                textBox1.Text = openFileDialog.FileName;
                //string folderPath = Path.GetDirectoryName(openFileDialog.FileName);
                //textBox1.Text = folderPath;
            }

        }

        private void button2_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderDialog = new FolderBrowserDialog();

            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                textBox2.Text = folderDialog.SelectedPath;
                saveFolderPath = folderDialog.SelectedPath;
            }
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            int totalFiles = listBox1.Items.Count;
            if (totalFiles == 0) return;

            progressBar1.Minimum = 0;
            progressBar1.Maximum = totalFiles;
            progressBar1.Value = 0;

            // Read values from UI
            string videoQuality = comboBox1.SelectedItem?.ToString();
            string soundQuality = comboBox2.SelectedItem?.ToString();
            string videoBitrate = comboBox3.SelectedItem?.ToString();
            string soundBitrate = comboBox4.SelectedItem?.ToString();
            string extension = comboBox5.SelectedItem?.ToString()?.Trim().ToLower();
            string videoCodecUI = comboBox6.SelectedItem?.ToString();
            bool keepVideo = checkBox1.Checked;
            bool keepSound = checkBox2.Checked;

            await Task.Run(() =>
            {
                foreach (string inputFile in listBox1.Items)
                {
                    ConvertFiles(inputFile, videoQuality, soundQuality, videoBitrate, soundBitrate, extension,videoCodecUI, keepVideo, keepSound);

                    // Update progress on UI thread
                    Invoke(new Action(() =>
                    {
                        progressBar1.Value += 1;
                    }));
                }
            });

            MessageBox.Show("Conversion complete!");
        }
        void ConvertFiles(string inputFile, string videoQuality, string soundQuality, string videoBitrate, string soundBitrate, string extension,string videoCodecUI, bool keepVideo, bool keepSound)
        {

            var ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe");

            // Build output file path
            string fileName = Path.GetFileNameWithoutExtension(inputFile);
            string outputFileName = fileName + "." + extension;
            string outputFile = Path.Combine(saveFolderPath, outputFileName);

            // Map quality to resolution and codec
            var (width, height) = GetVideoResolution(videoQuality);
            string audioCodec = GetAudioFormat(soundQuality);
            string videoCodec = GetVideoCodec(videoCodecUI);
            // Build FFMPEG arguments
            var args = new StringBuilder();
            args.Append($"-i \"{inputFile}\" ");

            if (keepVideo)
            {
                if (!string.IsNullOrEmpty(videoCodec))
                    args.Append($"-c:v {videoCodec} ");
                else args.Append("-c:v libx264 ");
                if (!string.IsNullOrEmpty(width) && !string.IsNullOrEmpty(height))
                    args.Append($"-vf scale={width}:{height} ");
                if (!string.IsNullOrEmpty(videoBitrate) && videoBitrate.ToLower() != "auto")
                    args.Append($"-b:v {videoBitrate}k ");
            }
            else
            {
                // Video disabled: strip all video streams
                args.Append("-vn ");
            }

            if (keepSound)
            {
                switch (audioCodec)
                {
                    case "aac":
                    case "libmp3lame":
                    case "libvorbis":
                    case "ac3":
                        args.Append($"-c:a {audioCodec} ");
                        if (!string.IsNullOrEmpty(soundBitrate) && soundBitrate.ToLower() != "auto")
                            args.Append($"-b:a {soundBitrate}k ");
                        break;

                    case "libopus":
                    case "alac":
                    case "flac":
                    case "pcm_s16le":
                    case "wavpack":
                    case "libopencore-amrnb":
                    case "libvo-amrwbenc":
                        args.Append($"-c:a {audioCodec} ");
                        // These codecs typically auto-manage bitrate or use other flags
                        break;

                    default:
                        // Auto-detect: omit -c:a to let FFmpeg choose default encoder
                        break;
                }
            }
            else
            {
                // Audio disabled: strip all audio streams
                args.Append("-an ");
            }

            args.Append($"\"{outputFile}\"");

            // Execute FFMPEG
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = args.ToString(),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = new System.Diagnostics.Process { StartInfo = startInfo })
            {
                process.Start();
                string output = process.StandardError.ReadToEnd();
                process.WaitForExit();
                File.WriteAllText(Path.Combine(saveFolderPath, "ffmpeg_log.txt"), output);

            }
        }
        (string width, string height) GetVideoResolution(string quality)
        {
            switch (quality)
            {
                case "Ultra HD (24K) 24576x12960":
                    return ("24576", "12960");
                case "Ultra HD (18K) 17280x9720":
                    return ("17280", "9720");
                case "Ultra HD (16K) 15360x8640":
                    return ("15360", "8640");
                case "Red Digital Cinema (12K) 12288x6480":
                    return ("12288", "6480");
                case "Ultra HD (10K) 10240x4320":
                    return ("10240", "4320");
                case "Ultra HD (8K) 7680x4320":
                    return ("7680", "4320");
                case "Apple Retina (5K) 5120x2880":
                    return ("5120", "2880");
                case "Ultra HD (4K) 3840×2160":
                    return ("3840", "2160");
                case "Full HD (1080p) 1920×1080":
                    return ("1920", "1080");
                case "HD (720p) 1280×720":
                    return ("1280", "720");
                case "SD (480p) 854×480":
                    return ("854", "480");
                case "Low (360p) 640×360":
                    return ("640", "360");
                default:
                    return ("640", "360");
            }
        }


        string GetAudioFormat(string format)
        {
            switch (format)
            {
                case "Lossless (WAV) ~1411 kbps":
                    return "pcm_s16le";
                case "Hi-Fi (FLAC) ~900-1100 kbps":
                    return "flac";
                case "High (MP3) 320 kbps":
                case "Medium (MP3) 192 kbps":
                case "Low (MP3) 128 kbps":
                    return "libmp3lame";
                case "High (AAC) 320 kbps":
                case "Medium (AAC) 192 kbps":
                case "Low (AAC) 128 kbps":
                    return "aac";
                case "Web (Opus) ~96 kbps":
                    return "libopus";
                case "Open (Vorbis) ~128 kbps":
                    return "libvorbis";
                case "Surround (AC-3) 384 kbps":
                    return "ac3";
                case "Apple Lossless (ALAC)":
                    return "alac";
                case "Hybrid (WavPack)":
                    return "wavpack";
                case "Mobile (AMR-NB)":
                    return "libopencore-amrnb";
                case "Mobile (AMR-WB)":
                    return "libvo-amrwbenc";
                case "Auto":
                    return null;
                default:
                    return null;
            }
        }

        string GetVideoCodec(string codecName)
        {
            switch (codecName)
            {
                case "H.264 (libx264)":
                    return "libx264";
                case "H.265 (libx265)":
                    return "libx265";
                case "VP8 (libvpx)":
                    return "libvpx";
                case "VP9 (libvpx-vp9)":
                    return "libvpx-vp9";
                case "AV1 (libaom-av1)":
                    return "libaom-av1";
                case "ProRes (prores)":
                    return "prores";
                case "MPEG-4 (mpeg4)":
                    return "mpeg4";
                case "MJPEG (mjpeg)":
                    return "mjpeg";
                case "DNxHD (dnxhd)":
                    return "dnxhd";
                case "Auto":
                    return null;
                default:
                    return null;
            }
        }
    }
}