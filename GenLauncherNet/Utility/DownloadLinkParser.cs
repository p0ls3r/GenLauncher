using System;
using System.Linq;

namespace GenLauncherNet
{
    public static class DownloadsLinkParser
    {
        public static string ParseDownloadLink(string link)
        {
            //replaced dl=0 to dl=1 to get download link
            //MyListBoxData.Add(new Modification("Rise of The Reds", "1.87 PB 2.0", "1.87 PB 2.0", "https://www.dropbox.com/s/nh8n8axi95gge41/ROTR.7z?dl=1", new BitmapImage(new Uri("Images/1.png", UriKind.Relative))));
            //generate from https://onedrive.live.com/?authkey=%21AIWtLuu54V5qKQ4&cid=896C9369E9176506&id=896C9369E9176506%21464&parId=896C9369E9176506%21463&o=OneUp
            //MyListBoxData.Add(new Modification("TEOD", "0.97.5", "0.97.5", "https://onedrive.live.com/download?cid=896C9369E9176506&resid=896C9369E9176506%21464&authkey=%21AIWtLuu54V5qKQ4"));
            //https://www.dropbox.com/s/ec9fjg909fkrvtt/TEOD.7z?dl=0            

            if (link.Contains("www.dropbox.com"))
                link = link.Replace("?dl=0", "?dl=1");

            if (link.Contains("https://onedrive.live.com"))
                link = HandleOneDriveLink(link);

            return link;
        }

        private static string HandleOneDriveLink(string link)
        {
            string result;
            if (link.Contains("embed"))
            {
                result = link.Replace("embed", "download");
            }
            else
            {
                var linkParts = link.Replace("https://onedrive.live.com/?", string.Empty).Split('&').ToList();

                var cid = linkParts.Where(t => t.Contains("cid=")).Select(t => t.Replace("cid=", string.Empty)).FirstOrDefault();
                var authKey = linkParts.Where(t => t.Contains("authkey=")).Select(t => t.Replace("authkey=", string.Empty)).FirstOrDefault();
                var resid = linkParts.Where(t => t.Contains("id=") && !t.Contains("cid=")).Select(t => t.Replace("id=", string.Empty)).FirstOrDefault();

                result = String.Format("https://onedrive.live.com/download?cid={0}&resid={1}&authkey={2}", cid, resid, authKey);
            }
            return result;
        }
    }
}