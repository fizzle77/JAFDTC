// ********************************************************************************************************************
//
// General.cs : general support
//
// Copyright(C) 2023 ilominar/raven
//
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU General
// Public License as published by the Free Software Foundation, either version 3 of the License, or (at your
// option) any later version.
//
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the
// implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License
// for more details.
//
// You should have received a copy of the GNU General Public License along with this program.  If not, see
// <https://www.gnu.org/licenses/>.
//
// ********************************************************************************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace JAFDTC.Utilities
{
    internal sealed class ClipboardData
    {
        public string SystemTag { get; set; }
        public string Data { get; set; }

        public ClipboardData(string systemTag, string data)
        {
            SystemTag = systemTag;
            Data = data;
        }
    }

    internal sealed class General
    {
        // TODO: document
        public static void PlayAudio(string filename)
        {
            MediaPlayer mediaPlayer = new()
            {
                Source = MediaSource.CreateFromUri(new Uri($"ms-appx:///Audio/{filename}"))
            };
            mediaPlayer.Play();
        }

        // TODO: document
        public static string JoinList(List<string> list)
        {
            var text = "";
            for (int i = 0; i < list.Count; i++)
            {
                if ((list.Count == 2) && (i == 1))
                {
                    text += " and ";
                }
                else if ((list.Count >= 2) && (i == (list.Count - 1)))
                {
                    text += ", and ";
                }
                else if ((list.Count >= 2) && (i > 0))
                {
                    text += ", ";
                }
                text += list[i];
            }
            return text;
        }

        // TODO: document
        public static async Task<ClipboardData> ClipboardDataAsync()
        {
            ClipboardData cboard = null;
            DataPackageView data = Clipboard.GetContent();
            if (data.Contains(StandardDataFormats.Text))
            {
                try
                {
                    string cboardData = await data.GetTextAsync();
                    using StringReader reader = new(cboardData);
                    string systemTag = reader.ReadLine();
                    cboard = new ClipboardData(systemTag, cboardData.Replace(systemTag, ""));
                }
                catch { }
            }
            return cboard;
        }

        // TODO: document
        public static void DataToClipboard(string systemTag, string data)
        {
            DataPackage dataPkg = new()
            {
                RequestedOperation = DataPackageOperation.Copy
            };
            dataPkg.SetText($"{systemTag}\n{data}");
            Clipboard.SetContent(dataPkg);

        }
    }
}
