using FellowOakDicom;
using Microsoft.Win32;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Input;
using ZapDICOMCleaner.FileReader;
using ZapDICOMCleaner.Helpers;

namespace ZapDICOMCleaner
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<string> _files = new List<string>();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnAddFiles_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.Multiselect = true;
            openFileDialog.Filter = "Zip files (*.zip)|*.zip|DICOM files (*.dcm)|*.dcm|All files (*.*)|*.*";
            var initialDirectory = AppSettings.Get("FilePath", Directory.GetCurrentDirectory());
            if (!Directory.Exists(initialDirectory))
            {
                initialDirectory = Directory.GetCurrentDirectory();
            }
            openFileDialog.InitialDirectory = initialDirectory;
            openFileDialog.FilterIndex = AppSettings.Get("FileFilterIndex", 1);

            if (openFileDialog.ShowDialog() == false)
            {
                return;
            }

            AppSettings.AddOrUpdate("FilePath", System.IO.Path.GetDirectoryName(openFileDialog.FileNames[0]));
            AppSettings.AddOrUpdate("FileFilterIndex", openFileDialog.FilterIndex.ToString());

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                var files = new List<string>();

                foreach (var filename in openFileDialog.FileNames)
                {
                    files.Add(filename);
                }

                foreach (var file in files)
                {
                    if (!_files.Contains(file))
                    {
                        _files.Add(file);
                    }
                }

                lbFiles.ItemsSource = null;
                lbFiles.ItemsSource = _files;

                btnClean.IsEnabled = _files.Count > 0;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }

        }

        private void btnAddFolders_Click(object sender, RoutedEventArgs e)
        {
            var openFolderDialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();

            openFolderDialog.Multiselect = true;

            if (openFolderDialog.ShowDialog() == false)
            {
                return;
            }

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                var files = new List<string>();

                foreach (var folder in openFolderDialog.SelectedPaths)
                {
                    files.AddRange(Directory.GetFiles(folder, "*.zip"));
                    files.AddRange(Directory.GetFiles(folder, "*.dcm"));
                }

                foreach (var file in files)
                {
                    if (!_files.Contains(file))
                    {
                        _files.Add(file);
                    }
                }

                lbFiles.ItemsSource = null;
                lbFiles.ItemsSource = _files;

                btnClean.IsEnabled = _files.Count > 0;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = lbFiles.SelectedItems;

            foreach (var selectedItem in selectedItems)
            {
                _files.Remove((string)selectedItem);
            }

            lbFiles.ItemsSource = null;
            lbFiles.ItemsSource = _files;

            btnClean.IsEnabled = _files.Count > 0;
        }

        private void btnClean_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                var files = new List<string>(_files);
                var tagToRemove = new DicomTag(0x0010, 0x1002);

                barProgress.Maximum = files.Count;
                barProgress.Value = 0;

                foreach (var file in files)
                {
                    lblActiveFile.Text = file;

                    if (Path.GetExtension(file).ToLower() == ".dcm")
                    {
                        if (cbOtherPatientIDsSequence.IsChecked == true)
                        {
                            var df = DicomFile.Open(file, FileReadOption.ReadAll);

                            if (df.Dataset.Contains(tagToRemove) && df.Dataset.GetSequence(tagToRemove).Items.Count == 0)
                            {
                                df.Dataset.Remove(tagToRemove);
                                df.Save(file);
                            }
                        }

                        _files.Remove(file);
                    }
                    else
                    {
                        var dicomFile = ExportZipFile.ReadDICOMRTStructFile(file);

                        if (dicomFile != null)
                        {
                            if (cbStructureVolume.IsChecked == true)
                                ConvertToLongStructureVolumeString(dicomFile);
                            if (cbContourCoordinates.IsChecked == true)
                                ConvertToLongCoordianteStrings(dicomFile);
                            if (cbContourColor.IsChecked == true)
                                ConvertToStructureColors(dicomFile);
                            if (cbStructureManyTypes.IsChecked == true)
                                ConvertAllContoursToCoplanar(dicomFile);

                            try
                            {
                                ExportZipFile.WriteDICOMRTStructFile(file, dicomFile);
                            }
                            catch
                            {
                                continue;
                            }

                            _files.Remove(file);
                        }
                    }

                    barProgress.Value++;
                }

                barProgress.Value = 0;
                lblActiveFile.Text = "";

                lbFiles.ItemsSource = null;
                lbFiles.ItemsSource = _files;

                btnClean.IsEnabled = _files.Count > 0;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void ConvertToLongCoordianteStrings(DicomFile dicomFile)
        {
            var seqStructs = dicomFile.Dataset.GetSequence(new FellowOakDicom.DicomTag(0x3006, 0x0039));

            foreach (var seq in seqStructs.Items)
            {
                var seqStruct = seq.GetSequence(new FellowOakDicom.DicomTag(0x3006, 0x0040));

                foreach (var seqSlide in seqStruct.Items)
                {
                    var numOfPoints = seqSlide.GetSingleValue<int>(new FellowOakDicom.DicomTag(0x3006, 0x0046));
                    var input = seqSlide.GetString(new FellowOakDicom.DicomTag(0x3006, 0x0050));

                    var output = ConvertStructureFile.ConvertLongNumbers(numOfPoints, input);

                    seqSlide.AddOrUpdate(new FellowOakDicom.DicomTag(0x3006, 0x0050), output);
                }
            }
        }

        private void ConvertAllContoursToCoplanar(DicomFile dicomFile)
        {
            var seqStructs = dicomFile.Dataset.GetSequence(new FellowOakDicom.DicomTag(0x3006, 0x0039));

            foreach (var seq in seqStructs.Items)
            {
                var seqStruct = seq.GetSequence(new FellowOakDicom.DicomTag(0x3006, 0x0040));

                foreach (var seqContour in seqStruct.Items)
                {
                    if (seqContour.GetString(new FellowOakDicom.DicomTag(0x3006, 0x0042)) == "POINT")
                        seqContour.AddOrUpdate(new FellowOakDicom.DicomTag(0x3006, 0x0042), "CLOSED_PLANAR");
                }
            }
        }

        private void ConvertToLongStructureVolumeString(DicomFile dicomFile)
        {
            var seqStructs = dicomFile.Dataset.GetSequence(new FellowOakDicom.DicomTag(0x3006, 0x0020));

            foreach (var seq in seqStructs.Items)
            {
                var input = seq.GetString(new FellowOakDicom.DicomTag(0x3006, 0x002C));
                var output = ConvertStructureFile.ConvertLongNumber(input);

                seq.AddOrUpdate(new FellowOakDicom.DicomTag(0x3006, 0x002C), output);
            }
        }

        private void ConvertToStructureColors(DicomFile dicomFile)
        {
            var seqStructs = dicomFile.Dataset.GetSequence(new FellowOakDicom.DicomTag(0x3006, 0x0039));

            foreach (var seq in seqStructs.Items)
            {
                var input = seq.GetString(new FellowOakDicom.DicomTag(0x3006, 0x002A));
                var output = ConvertStructureFile.ConvertColorString(input);

                seq.AddOrUpdate(new FellowOakDicom.DicomTag(0x3006, 0x002A), output);
            }
        }
    }
}