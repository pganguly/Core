﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace BigEgg.Framework.Applications
{
    /// <summary>
    /// This class encapsulates the logic for a recent file list.
    /// </summary>
    /// <remarks>
    /// This class can be used in a Settings file to store and load the recent file list as user settings. In Visual Studio you might need
    /// to enter the full class name "BigEgg.Framework.Applications.RecentFileList" in the "Select a type" dialog.
    /// </remarks>
    public sealed class RecentFileList : IXmlSerializable
    {
        private readonly ObservableCollection<RecentFile> recentFiles;
        private readonly ReadOnlyObservableCollection<RecentFile> readOnlyRecentFiles;
        private int maxFilesNumber = 8;
        

        /// <summary>
        /// Initializes a new instance of the <see cref="RecentFileList"/> class.
        /// </summary>
        public RecentFileList()
        {
            this.recentFiles = new ObservableCollection<RecentFile>();
            this.readOnlyRecentFiles = new ReadOnlyObservableCollection<RecentFile>(this.recentFiles);
        }


        /// <summary>
        /// Gets the list of recent files.
        /// </summary>
        public ReadOnlyObservableCollection<RecentFile> RecentFiles { get { return this.readOnlyRecentFiles; } }

        /// <summary>
        /// Gets or sets the maximum number of recent files in the list.
        /// </summary>
        /// <remarks>
        /// If the set number is lower than the list count then the recent file list is truncated to the number.
        /// </remarks>
        /// <exception cref="ArgumentException">The value must be equal or larger than 1.</exception>
        public int MaxFilesNumber
        {
            get { return this.maxFilesNumber; }
            set
            {
                if (this.maxFilesNumber != value)
                {
                    if (value <= 0) { throw new ArgumentException("The value must be equal or larger than 1."); }

                    this.maxFilesNumber = value;

                    if (this.recentFiles.Count - this.maxFilesNumber >= 1)
                    {
                        RemoveRange(this.maxFilesNumber, this.recentFiles.Count - this.maxFilesNumber);
                    }
                }
            }
        }

        private int PinCount { get { return this.recentFiles.Count(r => r.IsPinned); } }


        /// <summary>
        /// Loads the specified collection into the recent file list. Use this method when you need to initialize the RecentFileList 
        /// manually. This can be useful when you are using an own persistence implementation.
        /// </summary>
        /// <remarks>Recent file items that exist before the Load method is called are removed.</remarks>
        /// <param name="recentFiles">The recent files.</param>
        /// <exception cref="ArgumentNullException">The argument recentFiles must not be null.</exception>
        public void Load(IEnumerable<RecentFile> recentFiles)
        {
            if (recentFiles == null) { throw new ArgumentNullException("recentFiles"); }

            Clear();
            AddRange(recentFiles.Take(this.maxFilesNumber));
        }

        /// <summary>
        /// Adds a file to the recent file list. If the file already exists in the list then it only changes the position in the list.
        /// </summary>
        /// <param name="fileName">The path of the recent file.</param>
        /// <exception cref="ArgumentException">The argument fileName must not be null or empty.</exception>
        public void AddFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) { throw new ArgumentException("The argument fileName must not be null or empty."); }

            RecentFile recentFile = this.recentFiles.FirstOrDefault(r => r.Path == fileName);
            
            if (recentFile != null)
            {
                int oldIndex = this.recentFiles.IndexOf(recentFile);
                int newIndex = recentFile.IsPinned ? 0 : PinCount;
                if (oldIndex != newIndex)
                {
                    this.recentFiles.Move(oldIndex, newIndex);
                }
            }
            else
            {
                if (PinCount < this.maxFilesNumber)
                {
                    if (this.recentFiles.Count >= this.maxFilesNumber)
                    {
                        RemoveAt(this.recentFiles.Count - 1);
                    }
                    Insert(PinCount, new RecentFile(fileName));
                }
            }
        }

        XmlSchema IXmlSerializable.GetSchema() { return null; }

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            if (reader == null) { throw new ArgumentNullException("reader"); }

            reader.ReadToDescendant("RecentFile");
            while (reader.MoveToContent() == XmlNodeType.Element && reader.LocalName == "RecentFile")
            {
                RecentFile recentFile = new RecentFile();
                ((IXmlSerializable)recentFile).ReadXml(reader);
                Add(recentFile);
            }
            if (!reader.IsEmptyElement) { reader.ReadEndElement(); }
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            if (writer == null) { throw new ArgumentNullException("writer"); }

            foreach (RecentFile recentFile in this.recentFiles)
            {
                writer.WriteStartElement("RecentFile");
                ((IXmlSerializable)recentFile).WriteXml(writer);
                writer.WriteEndElement();
            }
        }

        private void Insert(int index, RecentFile recentFile)
        {
            recentFile.PropertyChanged += RecentFilePropertyChanged;
            this.recentFiles.Insert(index, recentFile);
        }
        
        private void Add(RecentFile recentFile)
        {
            recentFile.PropertyChanged += RecentFilePropertyChanged;
            this.recentFiles.Add(recentFile);
        }

        private void AddRange(IEnumerable<RecentFile> recentFilesToAdd)
        {
            foreach (RecentFile recentFile in recentFilesToAdd)
            {
                Add(recentFile);
            }
        }

        private void RemoveAt(int index)
        {
            this.recentFiles[index].PropertyChanged -= RecentFilePropertyChanged;
            this.recentFiles.RemoveAt(index);
        }

        private void RemoveRange(int index, int count)
        {
            for (int i = 0; i < count; i++)
            {
                RemoveAt(index);
            }
        }

        private void Clear()
        {
            foreach (RecentFile recentFile in this.recentFiles)
            {
                recentFile.PropertyChanged -= RecentFilePropertyChanged;
            }
            this.recentFiles.Clear();
        }

        private void RecentFilePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsPinned")
            {
                RecentFile recentFile = (RecentFile)sender;
                int oldIndex = this.recentFiles.IndexOf(recentFile);
                if (recentFile.IsPinned)
                {
                    this.recentFiles.Move(oldIndex, 0);
                }
                else
                {
                    int newIndex = PinCount;
                    if (oldIndex != newIndex)
                    {
                        this.recentFiles.Move(oldIndex, newIndex);
                    }
                }
            }
        }
    }
}