﻿namespace Selenium.WebDriver.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using OpenQA.Selenium;
    using OpenQA.Selenium.Internal;
    using OpenQA.Selenium.Remote;
    using Selenium.WebDriver.Extensions.Core;

    /// <summary>
    /// Searches the DOM elements using jQuery selector.
    /// </summary>
    public class JQuerySelector2 : OpenQA.Selenium.By
    {
        public JQuerySelector2(string selector, JQuerySelector2 jQueryContext = null, string jQueryVariable = "jQuery")
        {
            if (selector == null)
            {
                throw new ArgumentNullException("selector");
            }

            if (selector.IsNullOrWhiteSpace())
            {
                throw new ArgumentException("Selector cannot be empty", "selector");
            }

            if (jQueryVariable == null)
            {
                throw new ArgumentNullException("jQueryVariable");
            }

            if (jQueryVariable.IsNullOrWhiteSpace())
            {
                throw new ArgumentException("jQuery variable cannot be empty", "jQueryVariable");
            }

            this.RawSelector = selector;
            this.JQueryContext = jQueryContext;
            this.JQueryVariable = jQueryVariable;
            this.Selector = this.JQueryVariable + "('" + selector.Replace('\'', '"') + "'"
                + (this.JQueryContext != null ? ", " + this.JQueryContext : string.Empty) + ")";

            this.FindElementMethod = searchContext =>
            {
                var results = this.FindElements(searchContext);
                if (results.Count > 0)
                {
                    return results.First();
                }

                throw new NoSuchElementException("No element found for selector: " + this.RawSelector);
            };

            this.FindElementsMethod = searchContext =>
            {
                var driver = searchContext as IWebDriver;
                if (driver == null)
                {
                    var driverWrapper = searchContext as RemoteWebElement;
                    if (driverWrapper != null && driverWrapper.WrappedDriver != null)
                    {
                        var scr = @"return (function(element) {
                            'use strict';
                            var stack = [], siblingsCount, siblingIndex, i, sibling;
                            while (element.parentNode !== null) {
                                siblingsCount = 0;
                                siblingIndex = 0;
                                for (i = 0; i < element.parentNode.childNodes.length; i += 1) {
                                    sibling = element.parentNode.childNodes[i];
                                    if (sibling.nodeName === element.nodeName) {
                                        if (sibling === element) {
                                            siblingIndex = siblingsCount;
                                        }
                                        siblingsCount += 1;
                                    }
                                }
                                if (element.hasAttribute('id') && element.id !== '') {
                                    stack.unshift(element.nodeName.toLowerCase() + '#' + element.id);
                                } else if (siblingsCount > 1) {
                                    stack.unshift(element.nodeName.toLowerCase() + ':eq(' + siblingIndex + ')');
                                } else {
                                    stack.unshift(element.nodeName.toLowerCase());
                                }
                                element = element.parentNode;
                            }
                            stack = stack.slice(1); // removes the html element
                            return stack.join(' > ');
                        })(arguments[0])";
                        var res = ((IJavaScriptExecutor) driverWrapper.WrappedDriver).ExecuteScript(scr, driverWrapper);
                        driver = driverWrapper.WrappedDriver;
                    }
                    else
                    {
                        throw new NotSupportedException("Context is not a valid driver");
                    }
                }

                driver.JQuery().Load();
                var result = ParseUtil.ParseResult<IEnumerable<IWebElement>>(
                    driver.ExecuteScript<object>("return " + this.Selector + ".get();"));
                if (result == null)
                {
                    return new ReadOnlyCollection<IWebElement>(new List<IWebElement>());
                }
                else
                {
                    return new ReadOnlyCollection<IWebElement>(result.ToList());
                }
            };
        }

        /// <summary>
        /// Gets the query raw selector.
        /// </summary>
        public virtual string RawSelector { get; private set; }

        /// <summary>
        /// Gets or sets the selector.
        /// </summary>
        public virtual string Selector { get; protected set; }

        /// <summary>
        /// Gets the DOM Element, Document, or jQuery to use as context.
        /// </summary>
        public virtual JQuerySelector2 JQueryContext { get; private set; }

        /// <summary>
        /// Gets the variable that has been assigned to jQuery.
        /// </summary>
        public virtual string JQueryVariable { get; private set; }
    }
}
