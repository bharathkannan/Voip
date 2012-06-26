/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml;
using System.Xml.Linq;

using System.Reflection;

namespace System.Net.XMPP
{
    /// <summary>
    /// jabber:x:data form XEP-0004
    /// <x xmlns='jabber:x:data'   type='{form-type}'>  
    ///     <title/>  
    ///     <instructions/>  
    ///     <field var='field-name' type='{field-type}' label='description'>
    ///         <desc/>    
    ///         <required/>    
    ///         <value>field-value</value>    
    ///         <option label='option-label'><value>option-value</value></option>    
    ///         <option label='option-label'><value>option-value</value></option>  
    ///      </field>
    ///  </x>
    /// </summary>
    public class DataForm
    {
        public DataForm()
        {
        }
        public DataForm(string strTitle, string strInstr)
        {
            Title = strTitle;
            Instructions = strInstr;
        }


        private string m_strTitle = null;
        public string Title
        {
            get { return m_strTitle; }
            set { m_strTitle = value; }
        }

        private string m_strInstructions = null;
        public string Instructions
        {
            get { return m_strInstructions; }
            set { m_strInstructions = value; }
        }

        private string m_strType = "submit";

        public string Type
        {
            get { return m_strType; }
            set { m_strType = value; }
        }

        private string m_strFormType = null;
        [TextSingleFormField("FORM_TYPE", "", false, Type="hidden")]
        public string FormType
        {
            get { return m_strFormType; }
            set { m_strFormType = value; }
        }

        /// <summary>
        /// Builds
        /// </summary>
        /// <param name="objForm"></param>
        /// <returns></returns>
        public string BuildAskingForm(object objForm)
        {
            XNamespace xn = "jabber:x:data";
            XDocument doc = new XDocument();

            XElement elemMessage = new XElement(xn + "x");
            elemMessage.Add(new XAttribute("type", Type));

            doc.Add(elemMessage);

            if (Title != null)
                elemMessage.Add(new XElement(xn + "title", Title));
            if (Instructions != null)
                elemMessage.Add(new XElement(xn + "instructions", Instructions));
                


            /// Now build all our Properties
            /// 
            Type formtype = objForm.GetType();
            PropertyInfo [] props = formtype.GetProperties();
            if ((props != null) && (props.Length > 0))
            {
                foreach (PropertyInfo prop in props)
                {
                    object objPropValue = prop.GetValue(objForm, null);
                    
                    /// See what attributes we have
                    /// 
                    object [] attr = prop.GetCustomAttributes(typeof(FormFieldAttribute), true);
                    if ((attr == null) || (attr.Length <= 0))
                        continue;

                    FormFieldAttribute ffa = attr[0] as FormFieldAttribute;
                    ffa.AddXML(elemMessage, objPropValue);
                }
            }

            return doc.ToString(SaveOptions.None);
        }


        public string BuildSubmitForm(object objForm)
        {
            return "";
        }
        public string BuildCancelForm(object objForm)
        {
            return "";
        }
        public string BuildResultForm(object objForm)
        {
            return "";
        }

        /// <summary>
        ///  Parses this DataForm derived class from this provide xml node.  The node passed in must be x
        /// </summary>
        /// <param name="xlem"></param>
        public void ParseFromXML(XElement xlem)
        {
            if (xlem.Name == "{jabber:x:data}x")
            {
                XAttribute attr = xlem.Attribute("type");
                if (attr != null)
                    this.Type = attr.Value;

                var titles = xlem.Descendants("{jabber:x:data}title");
                foreach (XElement nexttitle in titles)
                {
                    this.Title = nexttitle.Value;
                    break;
                }
                var instructions = xlem.Descendants("{jabber:x:data}instructions");
                foreach (XElement nextinst in instructions)
                {
                    this.Instructions = nextinst.Value;
                    break;
                }

                var fields = xlem.Descendants("{jabber:x:data}field");
                foreach (XElement nextfield in fields)
                {
                    XAttribute attrvar = nextfield.Attribute("var");
                    if (attrvar != null)
                    {
                        SetPropertyFromFormValue(nextfield, attrvar.Value);
                    }
                }
                
            }
        }

        public void SetPropertyFromFormValue(XElement elemfield, string strVarValue)
        {
            List<string> Values = new List<string>();
            var values = elemfield.Descendants("{jabber:x:data}value");
            foreach (XElement nextvalue in values)
            {
                Values.Add(nextvalue.Value);
            }

            if (Values.Count > 0)
            {
                PropertyInfo [] props = GetType().GetProperties();
                foreach (PropertyInfo prop in props)
                {
                    /// See what attributes we have
                    /// 
                    object[] attr = prop.GetCustomAttributes(typeof(FormFieldAttribute), true);
                    if ((attr == null) || (attr.Length <= 0))
                        continue;

                    FormFieldAttribute ffa = attr[0] as FormFieldAttribute;
                    if (ffa.Var == strVarValue)
                    {
                        if (ffa.IsStringList == true)
                            prop.SetValue(this, Values, null);
                        else
                        {
                            prop.SetValue(this, Values[0], null);
                        }
                        break;
                    }
                    
                }
            }

        }

        //public static string BuildFormRequest(object objForm)
        //{

        //}
    }
}
