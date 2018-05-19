using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using DevExpress.CodeParser;
using DevExpress.CodeParser.CSharp;
using DevExpress.LookAndFeel;
using DevExpress.Skins;
using DevExpress.XtraRichEdit;
using DevExpress.XtraRichEdit.API.Native;
using DevExpress.XtraRichEdit.Services;

namespace SyntaxHighlightSimple
{

    public partial class Form1 : Form
    {
        public Form1()
        {
            string path = "Form1.cs";
            InitializeComponent();
            
            // Use service substitution to register a custom service that implements highlighting.
            ISyntaxHighlightService service = richEditControl1.GetService<ISyntaxHighlightService>();
            MySyntaxHighlightService svc = new MySyntaxHighlightService(richEditControl1);
            richEditControl1.RemoveService(typeof(ISyntaxHighlightService));
            richEditControl1.AddService(typeof(ISyntaxHighlightService), svc);
            
            richEditControl1.LoadDocument(path);
        }
    }
    /// <summary>
    ///  This class implements the Execute method of the ISyntaxHighlightService interface to parse and colorize the text.
    /// </summary>
    public class MySyntaxHighlightService : ISyntaxHighlightService
    {
        readonly RichEditControl syntaxEditor;
        SyntaxColors syntaxColors;
        SyntaxHighlightProperties commentProperties;
        SyntaxHighlightProperties keywordProperties;
        SyntaxHighlightProperties stringProperties;
        SyntaxHighlightProperties xmlCommentProperties;
        SyntaxHighlightProperties textProperties;

        public MySyntaxHighlightService(RichEditControl syntaxEditor)
        {
            this.syntaxEditor = syntaxEditor;
            syntaxColors = new SyntaxColors(UserLookAndFeel.Default);
        }

        void HighlightSyntax(TokenCollection tokens)
        {
            commentProperties = new SyntaxHighlightProperties();
            commentProperties.ForeColor = syntaxColors.CommentColor;

            keywordProperties = new SyntaxHighlightProperties();
            keywordProperties.ForeColor = syntaxColors.KeywordColor;

            stringProperties = new SyntaxHighlightProperties();
            stringProperties.ForeColor = syntaxColors.StringColor;

            xmlCommentProperties = new SyntaxHighlightProperties();
            xmlCommentProperties.ForeColor = syntaxColors.XmlCommentColor;

            textProperties = new SyntaxHighlightProperties();
            textProperties.ForeColor = syntaxColors.TextColor;

            if (tokens == null || tokens.Count == 0)
                return;

            Document document = syntaxEditor.Document;
            //CharacterProperties cp = document.BeginUpdateCharacters(0, 1);
            List<SyntaxHighlightToken> syntaxTokens = new List<SyntaxHighlightToken>(tokens.Count);
            foreach (Token token in tokens) {
                HighlightCategorizedToken((CategorizedToken)token, syntaxTokens);
            }            
            document.ApplySyntaxHighlight(syntaxTokens);
            //document.EndUpdateCharacters(cp);
        }
        void HighlightCategorizedToken(CategorizedToken token, List<SyntaxHighlightToken> syntaxTokens)
        {
            Color backColor = syntaxEditor.ActiveView.BackColor;
            TokenCategory category = token.Category;
            if (category == TokenCategory.Comment)
                syntaxTokens.Add(SetTokenColor(token, commentProperties, backColor));
            else if (category == TokenCategory.Keyword)
                syntaxTokens.Add(SetTokenColor(token, keywordProperties, backColor));
            else if (category == TokenCategory.String)
                syntaxTokens.Add(SetTokenColor(token, stringProperties, backColor));
            else if (category == TokenCategory.XmlComment)
                syntaxTokens.Add(SetTokenColor(token, xmlCommentProperties, backColor));
            else
                syntaxTokens.Add(SetTokenColor(token, textProperties, backColor));
        }
        SyntaxHighlightToken SetTokenColor(Token token, SyntaxHighlightProperties foreColor, Color backColor)
        {
            if (syntaxEditor.Document.Paragraphs.Count < token.Range.Start.Line)
                return null;
            int paragraphStart = DocumentHelper.GetParagraphStart(syntaxEditor.Document.Paragraphs[token.Range.Start.Line - 1]);
            int tokenStart = paragraphStart + token.Range.Start.Offset - 1;
            if (token.Range.End.Line != token.Range.Start.Line)
                paragraphStart = DocumentHelper.GetParagraphStart(syntaxEditor.Document.Paragraphs[token.Range.End.Line - 1]);

            int tokenEnd = paragraphStart + token.Range.End.Offset - 1;
            Debug.Assert(tokenEnd > tokenStart);
            return new SyntaxHighlightToken(tokenStart, tokenEnd - tokenStart, foreColor);
        }

        #region #ISyntaxHighlightServiceMembers
        public void Execute()
        {
            string newText = syntaxEditor.Text;
            // Use DevExpress.CodeParser to parse text into tokens.
            TokenCollection highlightTokens;
            highlightTokens = CSharpTokensHelper.GetTokens(newText);
            HighlightSyntax(highlightTokens);
        }

        public void ForceExecute()
        {
            Execute();
        }
        #endregion #ISyntaxHighlightServiceMembers
    }
    /// <summary>
    ///  This class provides colors to highlight the tokens.
    /// </summary>
    public class SyntaxColors
    {
        static Color DefaultCommentColor { get { return Color.Green; } }
        static Color DefaultKeywordColor { get { return Color.Blue; } }
        static Color DefaultStringColor { get { return Color.Brown; } }
        static Color DefaultXmlCommentColor { get { return Color.Gray; } }
        static Color DefaultTextColor { get { return Color.Black; } }
        UserLookAndFeel lookAndFeel;

        public Color CommentColor { get { return GetCommonColorByName(CommonSkins.SkinInformationColor, DefaultCommentColor); } }
        public Color KeywordColor { get { return GetCommonColorByName(CommonSkins.SkinQuestionColor, DefaultKeywordColor); } }
        public Color TextColor { get { return GetCommonColorByName(CommonColors.WindowText, DefaultTextColor); } }
        public Color XmlCommentColor { get { return GetCommonColorByName(CommonColors.DisabledText, DefaultXmlCommentColor); } }
        public Color StringColor { get { return GetCommonColorByName(CommonSkins.SkinWarningColor, DefaultStringColor); } }

        public SyntaxColors(UserLookAndFeel lookAndFeel)
        {
            this.lookAndFeel = lookAndFeel;
        }

        Color GetCommonColorByName(string colorName, Color defaultColor)
        {
            Skin skin = CommonSkins.GetSkin(lookAndFeel);
            if (skin == null)
                return defaultColor;
            return skin.Colors[colorName];
        }
    }
}

