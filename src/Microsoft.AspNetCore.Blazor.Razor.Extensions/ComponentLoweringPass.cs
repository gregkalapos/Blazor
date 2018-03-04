﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Blazor.Razor
{
    internal class ComponentLoweringPass : IntermediateNodePassBase, IRazorOptimizationPass
    {
        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
        {
            var @namespace = documentNode.FindPrimaryNamespace();
            var @class = documentNode.FindPrimaryClass();
            if (@namespace == null || @class == null)
            {
                // Nothing to do, bail. We can't function without the standard structure.
                return;
            }

            // For each component *usage* we need to rewrite the tag helper node to map to the relevant component
            // APIs.
            var nodes = documentNode.FindDescendantNodes<TagHelperIntermediateNode>();
            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (node.TagHelpers.Count > 1)
                {
                    node.Diagnostics.Add(BlazorDiagnosticFactory.Create_MultipleComponents(node.Source, node.TagName, node.TagHelpers));
                }

                RewriteUsage(node, node.TagHelpers[0]);
            }
        }

        private void RewriteUsage(TagHelperIntermediateNode node, TagHelperDescriptor tagHelper)
        {
            if (tagHelper.Kind != ComponentTagHelperDescriptorProvider.ComponentTagHelperKind)
            {
                return;
            }

            // We need to surround the contents of the node with open and close nodes to ensure the component
            // is scoped correctly.
            node.Children.Insert(0, new ComponentOpenExtensionNode()
            {
                TypeName = tagHelper.GetTypeName(),
            });

            for (var i = node.Children.Count - 1; i >= 0; i--)
            {
                if (node.Children[i] is TagHelperBodyIntermediateNode bodyNode)
                {
                    // Replace with a node that we recognize so that it we can do proper scope tracking.
                    //
                    // Note that we force the body node to be last, this is done to push it after the
                    // attribute nodes. This gives us the ordering we want for the render tree.
                    node.Children.RemoveAt(i);
                    node.Children.Add(new ComponentBodyExtensionNode(bodyNode)
                    {
                        TagMode = node.TagMode,
                        TagName = node.TagName,
                    });
                }
            }

            node.Children.Add(new ComponentCloseExtensionNode());

            // Now we need to rewrite any set property nodes to call the appropriate AddAttribute api.
            for (var i = node.Children.Count - 1; i >= 0; i--)
            {
                if (node.Children[i] is TagHelperPropertyIntermediateNode propertyNode &&
                    propertyNode.TagHelper == tagHelper)
                {
                    // We don't support 'complex' content for components (mixed C# and markup) right now.
                    // It's not clear yet if Blazor will have a good scenario to use these constructs.
                    //
                    // This is where a lot of the complexity in the Razor/TagHelpers model creeps in and we
                    // might be able to avoid it if these features aren't needed.
                    if (propertyNode.Children.Count == 1 &&
                        propertyNode.Children[0] is HtmlAttributeIntermediateNode htmlNode &&
                        htmlNode.Children.Count > 1)
                    {
                        // This case can be hit for a 'string' attribute
                        node.Diagnostics.Add(BlazorDiagnosticFactory.Create_UnsupportedComplexContent(
                            propertyNode.Source,
                            propertyNode,
                            htmlNode.Children));
                        node.Children.RemoveAt(i);
                        continue;
                    }
                    if (propertyNode.Children.Count == 1 &&
                        propertyNode.Children[0] is CSharpExpressionIntermediateNode cSharpNode &&
                        cSharpNode.Children.Count > 1)
                    {
                        // This case can be hit when the attribute has an explicit @ inside, which
                        // 'escapes' any special sugar we provide for codegen.
                        node.Diagnostics.Add(BlazorDiagnosticFactory.Create_UnsupportedComplexContent(
                            propertyNode.Source,
                            propertyNode,
                            cSharpNode.Children));
                        node.Children.RemoveAt(i);
                        continue;
                    }
                    else if (propertyNode.Children.Count > 1)
                    {
                        node.Diagnostics.Add(BlazorDiagnosticFactory.Create_UnsupportedComplexContent(
                            propertyNode.Source,
                            propertyNode,
                            propertyNode.Children));
                        node.Children.RemoveAt(i);
                        continue;
                    }

                    node.Children[i] = new ComponentAttributeExtensionNode(propertyNode)
                    {
                        PropertyName = propertyNode.BoundAttribute.GetPropertyName(),
                    };
                }
            }

            // Add an error and remove any nodes that don't map to a component property.
            for (var i = node.Children.Count - 1; i >= 0; i--)
            {
                if (node.Children[i] is TagHelperHtmlAttributeIntermediateNode attributeNode)
                {
                    node.Diagnostics.Add(BlazorDiagnosticFactory.Create_UnboundComponentAttribute(
                        attributeNode.Source,
                        tagHelper.GetTypeName(),
                        attributeNode));
                    node.Children.RemoveAt(i);
                }
            }
        }
    }
}
