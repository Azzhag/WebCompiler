﻿using System;
using System.ComponentModel.Composition;
using System.IO;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace WebCompilerVsix.Listeners
{
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType("LESS")]
    [ContentType("SCSS")]
    [ContentType("CoffeeScript")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    class SourceFileCreationListener : IVsTextViewCreationListener
    {
        [Import]
        public IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; set; }

        [Import]
        public ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        private ITextDocument _document;

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            var textView = EditorAdaptersFactoryService.GetWpfTextView(textViewAdapter);

            if (TextDocumentFactoryService.TryGetTextDocument(textView.TextDataModel.DocumentBuffer, out _document))
            {
                _document.FileActionOccurred += DocumentSaved;
            }

            textView.Closed += TextviewClosed;
        }

        private void TextviewClosed(object sender, EventArgs e)
        {
            IWpfTextView view = (IWpfTextView)sender;

            if (view != null)
                view.Closed -= TextviewClosed;

            if (_document != null)
                _document.FileActionOccurred -= DocumentSaved;
        }

        private void DocumentSaved(object sender, TextDocumentFileActionEventArgs e)
        {
            if (e.FileActionType == FileActionTypes.ContentSavedToDisk)
            {
                var item = WebCompilerPackage._dte.Solution.FindProjectItem(e.FilePath);

                if (item != null && item.ContainingProject != null)
                {
                    string folder = ProjectHelpers.GetRootFolder(item.ContainingProject);
                    string configFile = Path.Combine(folder, FileHelpers.FILENAME);

                    ErrorList.CleanErrors(e.FilePath);

                    if (File.Exists(configFile))
                        CompilerService.SourceFileChanged(configFile, e.FilePath);
                }
            }
        }
    }
}
