using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using sly.parser.generator.visitor.dotgraph;
using sly.parser.syntax.tree;

namespace sly.parser.generator.visitor
{
    [ExcludeFromCodeCoverage]
    public class GraphVizEBNFSyntaxTreeVisitor<IN> where IN : struct
    {
        public DotGraph Graph { get; private set; }

        public GraphVizEBNFSyntaxTreeVisitor()
        {
            Graph = new DotGraph("syntaxtree", true);
        }

        private int NodeCounter = 0;


        private DotNode Leaf(SyntaxLeaf<IN> leaf)
        {
            if (leaf.Token.IsIndent)
            {
                return Leaf(leaf.Token.TokenID, "INDENT>>");
            }
            else if (leaf.Token.IsUnIndent)
            {
                return Leaf(leaf.Token.TokenID, "<<UNINDENT");
            }
            return Leaf(leaf.Token.TokenID, leaf.Token.Value);
        }

        private DotNode Leaf(IN type, string value)
        {
            string label = type.ToString();
            label += "\n";
            var esc = value.Replace("\"", "\\\"");
            label += "\\\"" + esc + "\\\"";
            var node = new DotNode(NodeCounter.ToString())
            {
                // Set all available properties
                Shape = "doublecircle",
                Label = label,
                FontColor = "",
                Style = "",
                Height = 0.5f
            };
            NodeCounter++;
            Graph.Add(node);
            return node;
        }

        public DotNode VisitTree(ISyntaxNode<IN> root)
        {
            Graph = new DotGraph("syntaxtree", true);
            return Visit(root);
        }

        private DotNode Node(string label)
        {
            var node = new DotNode(NodeCounter.ToString())
            {
                // Set all available properties
                Shape = "ellipse",
                Label = label,
                FontColor = "black",
                Style = null,
                Height = 0.5f
            };
            NodeCounter++;
            Graph.Add(node);
            return node;
        }

        protected DotNode Visit(ISyntaxNode<IN> n)
        {
            switch (n)
            {
                case SyntaxLeaf<IN> leaf:
                    return Visit(leaf);
                case GroupSyntaxNode<IN> node:
                    return Visit(node);
                case ManySyntaxNode<IN> node:
                    return Visit(node);
                case OptionSyntaxNode<IN> node:
                    return Visit(node);
                case SyntaxNode<IN> node:
                    return Visit(node);
                default:
                    return Leaf(default(IN),"NULL");
            }
        }

        private DotNode Visit(GroupSyntaxNode<IN> node)
        {
            return Visit(node as SyntaxNode<IN>);
        }

        private DotNode Visit(OptionSyntaxNode<IN> node)
        {
            var child = node.Children != null && node.Children.Any<ISyntaxNode<IN>>() ? node.Children[0] : null;
            if (child == null || node.IsEmpty)
            {
                return null;
            }

            return Visit(child);
        }

        private string GetNodeLabel(SyntaxNode<IN> node)
        {
            string label = node.Name;
            if (node.IsExpressionNode)
            {
                label = node.Operation.OperatorToken.ToString();
            }

            return label;
        }

        private DotNode Visit(SyntaxNode<IN> node)
        {
            DotNode result = null;


            var children = new List<DotNode>();

            foreach (var n in node.Children)
            {
                var v = Visit(n);

                children.Add(v);
            }

            if (node.IsByPassNode)
            {
                //result = children[0];
            }
            else
            {

                result = Node(GetNodeLabel(node));
                Graph.Add(result);
                children.ForEach(c =>
                {
                    if (c != null) // Prevent arrows with null destinations
                    {
                        var edge = new DotArrow(result, c)
                        {
                            // Set all available properties
                            ArrowHeadShape = "none"
                        };
                        Graph.Add(edge);
                    }
                });

            }


            return result;
        }

        private DotNode Visit(ManySyntaxNode<IN> node)
        {
            return Visit(node as SyntaxNode<IN>);
        }


        private DotNode Visit(SyntaxLeaf<IN> leaf)
        {
            if (leaf.Token.IsIndent)
            {
                return Leaf(leaf.Token.TokenID, "INDENT>>");
            }
            else if (leaf.Token.IsUnIndent)
            {
                return Leaf(leaf.Token.TokenID, "<<UNINDENT");
            }
            return Leaf(leaf.Token.TokenID, leaf.Token.Value);
        }
    }
}