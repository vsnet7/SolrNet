﻿#region license
// Copyright (c) 2007-2010 Mauricio Scheffer
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
//  
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using SolrNet.Utils;

namespace SolrNet.Impl.ResponseParsers
{
	/// <summary>
	/// Parses TermVector results from a query response
	/// </summary>
	/// <typeparam name="T">Document type</typeparam>
	public class TermVectorResultsParser<T> : ISolrResponseParser<T>
	{
		public void Parse(XDocument xml, AbstractSolrQueryResults<T> results)
		{
			results.Switch(query: r => Parse(xml, r),
						   moreLikeThis: F.DoNothing);
		}

		public void Parse(XDocument xml, SolrQueryResults<T> results)
		{
			var rootNode = xml.XPathSelectElement("response/lst[@name='termVectors']");
			if (rootNode != null)
				results.TermVectorResults = ParseDocuments(rootNode);
		}

		/// <summary>
		/// Parses term vector results
		/// </summary>
		/// <param name="rootNode"></param>
		/// <returns></returns>
		public TermVectorResults ParseDocuments(XElement rootNode)
		{
			var r = new TermVectorResults();
			var docNodes = rootNode.Elements("lst");
			foreach (var docNode in docNodes)
			{
				var docNodeName = docNode.Attribute("name").Value;

				if (docNodeName == "warnings") 
				{
					// TODO: warnings
				}
				if (docNodeName == "uniqueKeyFieldName")
				{
					//TODO: support for unique key field name
				}
				else 
				{
					var doc = ParseDoc(docNode);

					r.Add(doc);
				}
				
			}
			return r;
		}

		private TermVectorDocumentResult ParseDoc(XElement docNode)
		{
			var fieldNodes = docNode.Elements();
		    var uniqueKey = fieldNodes
		        .Where(x => x.Attribute("name").ValueOrNull() == "uniqueKey")
		        .Select(x => x.Value)
		        .FirstOrDefault();
		    var termVectorResults = fieldNodes
		        .Where(x => x.Attribute("name").ValueOrNull() == "includes")
		        .SelectMany(ParseField)
                .ToList();

            return new TermVectorDocumentResult(uniqueKey, termVectorResults);
		}
		
		private IEnumerable<TermVectorResult> ParseField(XElement fieldNode) {
		    return fieldNode.Elements()
		        .Select(termNode => ParseTerm(termNode, fieldNode.Attribute("name").Value));
		}

		private TermVectorResult ParseTerm(XElement termNode, string fieldName) {
		    var nameValues = termNode.Elements()
                .Select(e => new {name = e.Attribute("name").Value, value = e.Value})
                .ToList();

		    var tf = nameValues
		        .Where(x => x.name == "tf")
		        .Select(x => (int?) int.Parse(x.value))
		        .FirstOrDefault();

            var df = nameValues
		        .Where(x => x.name == "df")
		        .Select(x => (int?) int.Parse(x.value))
		        .FirstOrDefault();

            var tfidf = nameValues
		        .Where(x => x.name == "tf-idf")
		        .Select(x => (double?) double.Parse(x.value, CultureInfo.InvariantCulture.NumberFormat))
		        .FirstOrDefault();

		    var offsets = termNode.Elements().SelectMany(ParseOffsets).ToList();
            var positions = termNode.Elements().SelectMany(ParsePositions).ToList();

            return new TermVectorResult(fieldName, 
                term: termNode.Attribute("name").Value,
                tf: tf, df: df, tfIdf: tfidf, 
                offsets: offsets, positions: positions);
		}

		private IEnumerable<int> ParsePositions(XElement valueNode) {
		    return valueNode.Elements().Select(p => int.Parse(p.Value));
		}

		private IEnumerable<Offset> ParseOffsets(XElement valueNode) {
		    return from e in valueNode.Elements()
		           where e.Attribute("name").Value == "start"
		           select new Offset(start : int.Parse(e.Value), end : int.Parse(((XElement) e.NextNode).Value));
		}
	}
}