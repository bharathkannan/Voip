using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Net.XMPP;
using System.Text.RegularExpressions;
using System.ComponentModel;

namespace WPFXMPPClient
{
    /// <summary>
    /// Interaction logic for DialogControl.xaml
    /// </summary>
    public partial class DialogControl : UserControl, INotifyPropertyChanged
    {
        public DialogControl()
        {
            InitializeComponent();
            this.DataContextChanged += new DependencyPropertyChangedEventHandler(DialogControl_DataContextChanged);
        }

        void DialogControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != null)
            {
                if (OurRosterItem != null)
                {
                    OurRosterItem.Conversation.Messages.CollectionChanged -= new System.Collections.Specialized.NotifyCollectionChangedEventHandler(Messages_CollectionChanged);
                }
                OurRosterItem = e.NewValue as RosterItem;
                if (OurRosterItem != null)
                {
                    OurRosterItem.Conversation.Messages.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(Messages_CollectionChanged);
                    SetConversation();
                }
            }
        }

        void Messages_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            SetConversation();
        }

        private void TextBlockChat_TouchDown(object sender, TouchEventArgs e)
        {

        }

        private void TextBlockChat_TouchMove(object sender, TouchEventArgs e)
        {

        }

        private RosterItem m_objOurRosterItem = null;
        public RosterItem OurRosterItem
        {
            get { return m_objOurRosterItem; }
            set { m_objOurRosterItem = value; }
        }


        private bool m_bShowTimeStamps = false;

        public bool ShowTimeStamps
        {
            get { return m_bShowTimeStamps; }
            set { m_bShowTimeStamps = value; }
        }

        //public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text",
        // typeof(TextMessage), typeof(DialogControl), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, new PropertyChangedCallback(PropChan)));

        //static void PropChan(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        //{
        //    DialogControl control = obj as DialogControl;
            
        //    //control.Text = args.NewValue as TextMessage;
        //}



        //public TextMessage Text
        //{
        //    get { return (TextMessage)GetValue(TextProperty); }
        //    set
        //    {
        //        SetValue(TextProperty, value);
        //        SetConversationSingleMessage(value);
        //        FirePropertyChanged("Text");
        //    }
        //}


        Regex reghyperlink = new Regex(@"\w+\://\S+", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline);
        Paragraph MainParagraph = new Paragraph();

        public void SetConversation()
        {
            OurRosterItem = this.DataContext as RosterItem;
            if (OurRosterItem == null)
                return;

            MainParagraph.Inlines.Clear();
            //MainParagraph.TextIndent = 20;
            MainParagraph.Margin = new Thickness(20, 0, 0, 0);
            TextBlockChat.Document.Blocks.Clear();
            TextBlockChat.Document.Blocks.Add(MainParagraph);

            bool? LastValue = null;
            foreach (TextMessage msg in OurRosterItem.Conversation.Messages)
            {
                AddInlinesForMessage(msg, LastValue);
                LastValue = msg.Sent;
            }

            this.TextBlockChat.ScrollToEnd();
        }

        public void SetConversationSingleMessage(TextMessage msg)
        {
            MainParagraph.Inlines.Clear();
            MainParagraph.Margin = new Thickness(20, 0, 0, 0);
            TextBlockChat.Document.Blocks.Clear();
            TextBlockChat.Document.Blocks.Add(MainParagraph);

            if (msg != null)
                AddInlinesForMessage(msg, null);

            this.TextBlockChat.ScrollToEnd();
        }

        const double FontSizeFrom = 10.0f;
        const double FontSizeMessage = 14.0f;
        void AddInlinesForMessage(TextMessage msg, bool? LastSent)
        {
            if (msg.Message == null)
                return;

            bool AddImage = true;
            if (LastSent.HasValue == true)
            {
                if (LastSent.Value == msg.Sent)
                    AddImage = false;
            }


            Span msgspan = new Span();

            if (AddImage == true)
            {
                string strName = "leftarrow";
                if (msg.Sent == true)
                    strName = "rightarrow";
                GeometryDrawing geom = Application.Current.FindResource(strName) as GeometryDrawing;
                DrawingImage drawimage = new DrawingImage(geom);
                Image image = new Image();
                image.Source = drawimage;
                image.Stretch = Stretch.Fill;
                image.Width = 6;
                image.Height = 10;
                image.Margin = new Thickness(-20, 0, 0, 0);
                image.HorizontalAlignment = HorizontalAlignment.Left;
                image.VerticalAlignment = System.Windows.VerticalAlignment.Center;

                msgspan.Inlines.Add(image);
            }

            if (ShowTimeStamps == true)
            {
                string strRun = string.Format("{0} to {1} - {2}", msg.From, msg.To, msg.Received);
                Run runfrom = new Run(strRun);
                runfrom.Foreground = Brushes.Gray;
                runfrom.FontSize = FontSizeFrom;
                msgspan.Inlines.Add(runfrom);

                msgspan.Inlines.Add(new LineBreak());
            }

            /// Look for hyperlinks in our run
            /// 
            string strMessage = msg.Message;
            int nMatchAt = 0;
            Match matchype = reghyperlink.Match(strMessage, nMatchAt);
            while (matchype.Success == true)
            {
                string strHyperlink = matchype.Value;

                /// Add everything before this as a normal run
                /// 
                if (matchype.Index > nMatchAt)
                {
                    Run runtext = new Run(strMessage.Substring(nMatchAt, (matchype.Index - nMatchAt)));
                    runtext.Foreground = msg.TextColor;

                    if (runtext.Text.Length > 0) 
                        msgspan.Inlines.Add(runtext);
                }

                Hyperlink link = new Hyperlink();
                link.Inlines.Add(strMessage.Substring(matchype.Index, matchype.Length));
                link.Foreground = Brushes.Blue;
                link.TargetName = "_blank";
                try
                {
                    link.NavigateUri = new Uri(strMessage.Substring(matchype.Index, matchype.Length));
                }
                catch (Exception)
                {
                }
                link.Click += new RoutedEventHandler(link_Click);
                msgspan.Inlines.Add(link);

                nMatchAt = matchype.Index + matchype.Length;

                if (nMatchAt >= (strMessage.Length - 1))
                    break;

                matchype = reghyperlink.Match(strMessage, nMatchAt);
            }

            /// see if we have any remaining text
            /// 
            if (nMatchAt < strMessage.Length)
            {
                Run runtext = new Run(strMessage.Substring(nMatchAt, (strMessage.Length - nMatchAt)));
                runtext.Foreground = msg.TextColor;
                if (runtext.Text.Length > 0)
                {
                    msgspan.Inlines.Add(runtext);
                }
            }
            msgspan.Inlines.Add(new LineBreak());

            this.MainParagraph.Inlines.Add(msgspan);
        }

        void link_Click(object sender, RoutedEventArgs e)
        {
            /// Navigate to this link
            /// 
            System.Diagnostics.Process.Start(((Hyperlink)sender).NavigateUri.ToString());
        }


        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        void FirePropertyChanged(string strName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(strName));
        }
        #endregion

    }
}
