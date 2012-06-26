/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml;
using System.Xml.Linq;

namespace System.Net.XMPP
{

    public class FormOption
    {
        public FormOption(string strLabel, string strValue)
        {
            Label = strLabel;
            Value = strValue;
        }

        private string m_strLabel = "";
        public string Label
        {
            get { return m_strLabel; }
            set { m_strLabel = value; }
        }

        private string m_strValue = "";
        public string Value
        {
            get { return m_strValue; }
            set { m_strValue = value; }
        }
    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.Property, AllowMultiple = false)]
    public class FormFieldAttribute : Attribute
    {
        public FormFieldAttribute(string strVar, string strType, string strLabel, bool bRequired)
            : base()
        {
            Var = strVar;
            Type = strType;
            Label = strLabel;
            Required = bRequired;
        }

        public FormFieldAttribute(string strVar, string strType, string strLabel, bool bRequired, string strDescription)
            : base()
        {
            Var = strVar;
            Type = strType;
            Label = strLabel;
            Required = bRequired;
            Description = strDescription;
        }

        protected bool m_bIsStringList = false;

        public bool IsStringList
        {
            get { return m_bIsStringList; }
        }

        private string m_strVar = "";
        public string Var
        {
            get { return m_strVar; }
            set { m_strVar = value; }
        }

        private string m_strType = "";
        public string Type
        {
            get { return m_strType; }
            set { m_strType = value; }
        }

        private string m_strLabel = "";
        public string Label
        {
            get { return m_strLabel; }
            set { m_strLabel = value; }
        }

        private bool m_bRequired = false;

        public bool Required
        {
            get { return m_bRequired; }
            set { m_bRequired = value; }
        }

        private string m_strDescription = "";

        public string Description
        {
            get { return m_strDescription; }
            set { m_strDescription = value; }
        }

        /// <summary>
        ///  TODO.. need to get xml for form request or submit
        /// </summary>
        public virtual void AddXML(XElement parentnode, object InstanceData)
        {
            /// No XML if this field is null or empty and not required
            if ((Required == false) && ((InstanceData == null) || (InstanceData.ToString().Length <= 0)))
                return;

            /// Add the field element
            /// 
            XElement elemField = new XElement("{jabber:x:data}field", new XAttribute("type", Type), new XAttribute("var", Var));

            string strValue = GetValue(InstanceData);
            if (strValue != null)
            {
                elemField.Add(new XElement("{jabber:x:data}value", strValue));
            }
            parentnode.Add(elemField);
        }

        public virtual string GetValue(object InstanceData)
        {
            return InstanceData.ToString();
        }


    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.Property, AllowMultiple = false)]
    public class TextSingleFormFieldAttribute : FormFieldAttribute
    {
        public TextSingleFormFieldAttribute(string strVar, string strLabel, bool bRequired)
            : base(strVar, "text-single", strLabel, bRequired)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.Property, AllowMultiple = false)]
    public class TextPrivateFormFieldAttribute : FormFieldAttribute
    {
        public TextPrivateFormFieldAttribute(string strVar, string strLabel, bool bRequired)
            : base(strVar, "text-private", strLabel, bRequired)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.Property, AllowMultiple = false)]
    public class FixedFormFieldAttribute : FormFieldAttribute
    {
        public FixedFormFieldAttribute(string strVar, string strLabel, string strFixedValue, bool bRequired)
            : base(strVar, "fixed", strLabel, bRequired)
        {
        }

        private string m_strFixedValue = "";

        public string FixedValue
        {
            get { return m_strFixedValue; }
            set { m_strFixedValue = value; }
        }

        public override string GetValue(object InstanceData)
        {
            return FixedValue;
        }
    }

    /// <summary>
    /// Can only be applied to a string List
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.Property, AllowMultiple = false)]
    public class TextMultiFormFieldAttribute : FormFieldAttribute
    {
        public TextMultiFormFieldAttribute(string strVar, string strLabel, bool bRequired)
            : base(strVar, "text-multi", strLabel, bRequired)
        {
            m_bIsStringList = true;
        }



        public override void AddXML(XElement parentnode, object InstanceData)
        {
            /// No XML if this field is null or empty and not required
            if ((Required == false) && ((InstanceData == null) || (InstanceData.ToString().Length <= 0)))
                return;

            /// Add the field element
            /// 
            XElement elemField = new XElement("{jabber:x:data}field", new XAttribute("type", Type), new XAttribute("var", Var));

            IEnumerable<string> Values = (IEnumerable<string>)InstanceData;
            foreach (string strValue in Values)
            {
                elemField.Add(new XElement("{jabber:x:data}value", strValue));
            }
            parentnode.Add(elemField);
        }
      
    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.Property, AllowMultiple = false)]
    public class JIDMultiFormFieldAttribute : FormFieldAttribute
    {
        public JIDMultiFormFieldAttribute(string strVar, string strLabel, bool bRequired)
            : base(strVar, "jid-multi", strLabel, bRequired)
        {
            m_bIsStringList = true;
        }

        public override void AddXML(XElement parentnode, object InstanceData)
        {
            /// No XML if this field is null or empty and not required
            if ((Required == false) && ((InstanceData == null) || (InstanceData.ToString().Length <= 0)))
                return;

            IEnumerable<string> Values = (IEnumerable<string>)InstanceData;
            if ((Required == false) && (Values.Count() <= 0))
                return;

            /// Add the field element
            /// 
            XElement elemField = new XElement("{jabber:x:data}field", new XAttribute("type", Type), new XAttribute("var", Var));

            foreach (string strValue in Values)
            {
                elemField.Add(new XElement("{jabber:x:data}value", strValue));
            }
            parentnode.Add(elemField);
        }
    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.Property, AllowMultiple = false)]
    public class ListSingleFormFieldAttribute : FormFieldAttribute
    {
        public ListSingleFormFieldAttribute(string strVar, string strLabel, bool bRequired, string values, string labels)
            : base(strVar, "list-single", strLabel, bRequired)
        {
            if ((values != null) && (labels != null))
            {
                string[] saValues = values.Split(',');
                string[] saLabels = labels.Split(',');
                if (saValues.Length == saLabels.Length)
                {
                    List<FormOption> listop = new List<FormOption>();
                    for (int i = 0; i < saValues.Length; i++)
                    {
                        FormOption opt = new FormOption(saLabels[i].Trim(), saValues[i].Trim());
                        listop.Add(opt);
                    }
                    Options = listop.ToArray();
                }
            }
        }

        private FormOption[] m_aOptions = new FormOption[] { };
        public FormOption[] Options
        {
            get { return m_aOptions; }
            set { m_aOptions = value; }
        }

    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.Property, AllowMultiple = false)]
    public class ListMultiFormFieldAttribute : FormFieldAttribute
    {
        public ListMultiFormFieldAttribute(string strVar, string strLabel, bool bRequired, string values, string labels)
            : base(strVar, "list-multi", strLabel, bRequired)
        {
            m_bIsStringList = true;
            if ((values != null) && (labels != null))
            {
                string[] saValues = values.Split(',');
                string[] saLabels = labels.Split(',');
                if (saValues.Length == saLabels.Length)
                {
                    List<FormOption> listop = new List<FormOption>();
                    for (int i = 0; i < saValues.Length; i++)
                    {
                        FormOption opt = new FormOption(saLabels[i].Trim(), saValues[i].Trim());
                        listop.Add(opt);
                    }
                    Options = listop.ToArray();
                }
            }
        }

        private FormOption[] m_aOptions = new FormOption[] { };
        public FormOption[] Options
        {
            get { return m_aOptions; }
            set { m_aOptions = value; }
        }

        public override void AddXML(XElement parentnode, object InstanceData)
        {
            /// No XML if this field is null or empty and not required
            if ((Required == false) && ((InstanceData == null) || (InstanceData.ToString().Length <= 0)))
                return;

            IEnumerable<string> Values = (IEnumerable<string>)InstanceData;
            if ((Required == false) && (Values.Count() <= 0))
                return;

            /// Add the field element
            /// 
            XElement elemField = new XElement("{jabber:x:data}field", new XAttribute("type", Type), new XAttribute("var", Var));

            foreach (string strValue in Values)
            {
                elemField.Add(new XElement("{jabber:x:data}value", strValue));
            }
            parentnode.Add(elemField);
        }
       
    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.Property, AllowMultiple = false)]
    public class BoolFormFieldAttribute : FormFieldAttribute
    {
        public BoolFormFieldAttribute(string strVar, string strLabel, bool bRequired)
            : base(strVar, "boolean", strLabel, bRequired)
        {
        }

        public override string GetValue(object InstanceData)
        {
            bool bInstanceData = (bool)InstanceData;
            return bInstanceData ? "1" : "0";
        }
    }

}
