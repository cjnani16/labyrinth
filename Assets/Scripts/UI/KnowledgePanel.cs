using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using UnityEditor;

[System.Serializable]
public class KnowledgePanel : MonoBehaviour
{
    public GameObject EntityObject;
    public TMPro.TextMeshProUGUI ConsoleText;
    public TMPro.TMP_InputField NewSentenceField;
    BeAnEntity EntityIF;
    AIKit.Entity Entity;


    // Start is called before the first frame update
    void Start()
    {
        //Attach to an Entity;
        bool attachSuccessful = false;
        if (EntityIF = EntityObject.GetComponent<BeAnEntity>()) {
            Debug.Log("Knowledge panel attached to Entity " + EntityIF.EntityName);
            Entity = EntityIF.GetSelf();
            attachSuccessful = true;
        }
        else {
            Debug.LogError("Knowledge panel failed to attach to Entity! Target = " + EntityObject);
        }
        if (!attachSuccessful) return;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void GetEntailedByThis() 
    {
        if (Entity is null) return;

        string givenSentence = NewSentenceField.text;
        //Debug.Log("Initial Knowledge Base: \n" + entity.knowledgeModule.lexicalMemory.AllNodesInfo());
        AIKit.Sentence sentenceParsed = AIKit.AIKit_Grammar.Interpret(new List<string>(givenSentence.ToLower().Split(' ')));
        string s = "Parsed '"+givenSentence+"' to: "+sentenceParsed.GetSemantics().ToString();
        //Debug.Log(s);

        List<AIKit.SemSentence> returnedSentences = Entity.knowledgeModule.GetSentencesEntailedBy(sentenceParsed.GetSemantics());
        if (returnedSentences.Count > 0)
            ConsoleText.text = "Found " + returnedSentences.Count + " entailed sentences.\n" + string.Join("\n", returnedSentences.Select(m => m.ToString()).ToArray());
        else
            ConsoleText.text = "No sentences entailed by this.";
    }

    public void GetEntailThis() 
    {
        if (Entity is null) return;

        string givenSentence = NewSentenceField.text;
        //Debug.Log("Initial Knowledge Base: \n" + entity.knowledgeModule.lexicalMemory.AllNodesInfo());
        AIKit.Sentence sentenceParsed = AIKit.AIKit_Grammar.Interpret(new List<string>(givenSentence.ToLower().Split(' ')));
        string s = "Parsed '"+givenSentence+"' to: "+sentenceParsed.GetSemantics().ToString();
        //Debug.Log(s);

        List<AIKit.SemSentence> returnedSentences = Entity.knowledgeModule.GetSentencesThatEntail(sentenceParsed.GetSemantics());
        if (returnedSentences.Count > 0)
            ConsoleText.text = "Found "+returnedSentences.Count+" entailing sentences.\n\n" + string.Join("\n", returnedSentences.Select(m => m.ToString()).ToArray());
        else
            ConsoleText.text = "No sentences entail this.";
    }

    public void GetPlanTo()
    {
        if (Entity is null) return;

        string givenSentence = NewSentenceField.text;
        //Debug.Log("Initial Knowledge Base: \n" + entity.knowledgeModule.lexicalMemory.AllNodesInfo());
        AIKit.Sentence sentenceParsed = AIKit.AIKit_Grammar.Interpret(new List<string>(givenSentence.ToLower().Split(' ')));
        string s = "Parsed '"+givenSentence+"' to: "+sentenceParsed.GetSemantics().ToString();
        //Debug.Log(s);

        List<AIKit.SemSentence> returnedSentences = Entity.knowledgeModule.PlanTo(sentenceParsed.GetSemantics()).ToList();
        if (returnedSentences.Count > 0)
            ConsoleText.text = "Found " + returnedSentences.Count + " step plan.\n\n" + string.Join("\n", returnedSentences.Select(m => m.ToString()).ToArray());
        else
            ConsoleText.text = "Failed to plan how to do this.";
    }

    public void GetTruthOf()
    {
        if (Entity is null) return;

        string givenSentence = NewSentenceField.text;
        //Debug.Log("Initial Knowledge Base: \n" + entity.knowledgeModule.lexicalMemory.AllNodesInfo());
        AIKit.Sentence sentenceParsed = AIKit.AIKit_Grammar.Interpret(new List<string>(givenSentence.ToLower().Split(' ')));
        string s = "Parsed '"+givenSentence+"' to: "+sentenceParsed.GetSemantics().ToString();
        //Debug.Log(s);

        string output;
        ConsoleText.text = Entity.knowledgeModule.isTrue(sentenceParsed, out output) ? "This is true.\n" : "This is false.\n";
        ConsoleText.text += output;
    }

    public void GetWaysTo() {
        if (Entity is null) return;

        string givenSentence = NewSentenceField.text;
        //Debug.Log("Initial Knowledge Base: \n" + entity.knowledgeModule.lexicalMemory.AllNodesInfo());
        AIKit.Sentence sentenceParsed = AIKit.AIKit_Grammar.Interpret(new List<string>(givenSentence.ToLower().Split(' ')));
        string s = "Parsed '"+givenSentence+"' to: "+sentenceParsed.GetSemantics().ToString();
        //Debug.Log(s);

        List<AIKit.SemSentence> returnedSentences = Entity.knowledgeModule.GetWaysTo(sentenceParsed.GetSemantics());
        if (returnedSentences.Count > 0)
            ConsoleText.text = "Found " + returnedSentences.Count + " ways to do this.\n" + string.Join("\n", returnedSentences.Select(m => m.ToString()).ToArray());
        else
            ConsoleText.text = "No ways to do this.";
    }

    public void GetResultsFrom() {
        if (Entity is null) return;

        string givenSentence = NewSentenceField.text;
        //Debug.Log("Initial Knowledge Base: \n" + entity.knowledgeModule.lexicalMemory.AllNodesInfo());
        AIKit.Sentence sentenceParsed = AIKit.AIKit_Grammar.Interpret(new List<string>(givenSentence.ToLower().Split(' ')));
        string s = "Parsed '"+givenSentence+"' to: "+sentenceParsed.GetSemantics().ToString();
        //Debug.Log(s);

        List<AIKit.SemSentence> returnedSentences = Entity.knowledgeModule.GetResultsFrom(sentenceParsed.GetSemantics());
        if (returnedSentences.Count > 0)
            ConsoleText.text = "Found " + returnedSentences.Count + " results from this.\n" + string.Join("\n", returnedSentences.Select(m => m.ToString()).ToArray());
        else
            ConsoleText.text = "No results from this.";
    }
}







/////TRYING TO USE GRAPHVIEW
///


public class TestNodeElement : Node
{
    AIKit.SemanticWebNode node;
    public TestNodeElement(AIKit.SemanticWebNode testNode)
    {
        node = testNode;
        name = node.GetAliases()[0].ToString();
    }

    public void InitializeNode()
    {
        //This was a big part of the issue, right here. In custom nodes, this doesn't get called automatically.
        //Short of supplying your own stylesheet that covers all the bases, this needs to be explicitly called to give a node visible attributes.
        UseDefaultStyling();

        VisualElement contents = this.Q<VisualElement>("contents");

        //Button contentsButton = new Button(() => { Debug.Log("Clicked!"); });
        //contentsButton.text = contentsButton.name = node.ToString();
        //contents.Add(contentsButton);
        title = name;

        SetPosition(new Rect(50, 50, 0, 0));

        MarkDirtyRepaint();
    }
}

public class TestGraphView : GraphView
{
    public AIKit.LexicalMemory graph;
    public TestGraphView() { }
}

public class TestGraphWindow : EditorWindow
{
    [MenuItem("Graph/Knowledge Graph")]
    public static void OpenWindow()
    {
        TestGraphWindow wnd = GetWindow<TestGraphWindow>();
        wnd.titleContent = new GUIContent("Knowledge Graph for " + Selection.activeGameObject.GetComponent<BeAnEntity>().EntityName);
        wnd.graphObject = Selection.activeGameObject.GetComponent<BeAnEntity>().GetSelf().knowledgeModule.lexicalMemory;
        Debug.Log("Oopening knowledge window for " + Selection.activeGameObject.GetComponent<BeAnEntity>().EntityName);
        wnd.CreateElements();
    }

    AIKit.LexicalMemory graphObject;

    private void CreateElements()
    {
        rootVisualElement.Clear();

        TestGraphView graphView = new TestGraphView() { name = "Semantic Web", viewDataKey = "TestGraphView", graph = graphObject };

        graphView.SetupZoom(0.05f, ContentZoomer.DefaultMaxScale);

        graphView.AddManipulator(new ContentDragger());
        graphView.AddManipulator(new SelectionDragger());
        graphView.AddManipulator(new RectangleSelector());
        graphView.AddManipulator(new ClickSelector());

        graphView.RegisterCallback<KeyDownEvent>(KeyDown);

        graphView.graphViewChanged = GraphViewChanged;

        rootVisualElement.Add(graphView);
        graphView.StretchToParentSize();

        GridBackground gridBackground = new GridBackground() { name = "Grid" };
        graphView.Add(gridBackground);
        gridBackground.SendToBack();

        //place all nodes
        foreach (AIKit.SemanticWebNode semanticWebNode in graphObject.GetAllNodes())
        {
            TestNodeElement newGraphNode = new TestNodeElement(semanticWebNode) { name = semanticWebNode.GetAliases()[0].ToString() };

            newGraphNode.userData = semanticWebNode;

            newGraphNode.RefreshExpandedState();
            newGraphNode.RefreshPorts();

            graphView.AddElement(newGraphNode);
            newGraphNode.InitializeNode();
            rootVisualElement.MarkDirtyRepaint();
        }

        //add edges
        foreach (var graphNode in graphView.nodes.ToList())
        {
            AIKit.SemanticWebNode semanticWebNode = graphNode.userData as AIKit.SemanticWebNode;

            foreach (AIKit.SemanticWebEdge edge in semanticWebNode.GetEdges())
            {
                string outPortName = edge.word.ToString() + ((edge.to is null) ? "." : "->");
                Port outPort = graphNode.outputContainer.Q<Port>(name: outPortName);
                if (outPort is null)
                {
                    outPort = graphNode.InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
                    outPort.portName = outPortName;
                    outPort.name = outPortName;
                    outPort.portColor = Color.green;
                    graphNode.outputContainer.Add(outPort);
                }

                if (edge.to is null)
                {
                    Debug.LogError("Edge '" + edge.from.GetAliases()[0] + " " + edge.word.ToString() + "' has no 'to' node.");
                    continue;
                }


                Node targetGraphNode = graphView.nodes.ToList().Find((node) =>
                {
                    if ((node.userData as AIKit.SemanticWebNode) is null) { Debug.LogError("strange null in userData"); }
                    Debug.Log("LF: " + (node.userData as AIKit.SemanticWebNode).GetAliases()[0]);
                    Debug.Log("Found: " + edge.to.GetAliases()[0]);
                    return node.userData == edge.to;
                });
                string inPortName = "->" + edge.word.ToString();
                Port inPort = targetGraphNode.inputContainer.Q<Port>(name: inPortName);
                if (inPort is null)
                {
                    inPort = targetGraphNode.InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
                    inPort.name = inPortName;
                    inPort.portName = inPortName;
                    inPort.portColor = Color.red;
                    targetGraphNode.inputContainer.Add(inPort);
                }

                if (inPort is null || outPort is null)
                {
                    Debug.LogError("Underfined target or source node! ...");
                    Debug.LogError("...for edge "+ edge.from.ToString() + edge.word.ToString() + edge.to.ToString() +"!");
                }

                Edge e = outPort.ConnectTo(inPort);
                e.name = edge.word.ToString();
                e.tooltip = edge.from.GetString() + " " + edge.word + " " + edge.to.GetString();
                e.edgeControl.outputColor = Color.red;
                e.edgeControl.inputColor = Color.blue;
                e.styleSheets.Add(Resources.Load<StyleSheet>("Graph"));

                e.MarkDirtyRepaint();

                graphView.AddElement(e);
            }

            graphNode.RefreshExpandedState();
            graphNode.RefreshPorts();
            graphNode.MarkDirtyRepaint();
            rootVisualElement.MarkDirtyRepaint();
        }

        //layout using MSAGL
        ApplyMSAGLPosition(graphView.nodes.ToList());
    }

    private static void ApplyMSAGLPosition(List<Node> graphViewNodes)
    {
        Microsoft.Msagl.Core.Layout.GeometryGraph graph = new Microsoft.Msagl.Core.Layout.GeometryGraph();

        //store semanticwebnode + msagl node
        List<(Microsoft.Msagl.Core.Layout.Node, AIKit.SemanticWebNode)> nodePairs = new List<(Microsoft.Msagl.Core.Layout.Node, AIKit.SemanticWebNode)>();

        //place all nodes
        int n = 0;
        foreach (var node in graphViewNodes)
        {
            Debug.Log("MSAGL Loading node " + (n++) + "...");

            Microsoft.Msagl.Core.Layout.Node msNode = new Microsoft.Msagl.Core.Layout.Node(

            Microsoft.Msagl.Core.Geometry.Curves.CurveFactory.CreateRectangle(
                10,
                10,
                new Microsoft.Msagl.Core.Geometry.Point())
            ,node.userData);

            graph.Nodes.Add(msNode);
            nodePairs.Add((msNode, (node.userData as AIKit.SemanticWebNode)));
        }

        //add edges
        foreach (var node in graph.Nodes)
        {
            foreach (AIKit.SemanticWebEdge edge in (node.UserData as AIKit.SemanticWebNode).GetEdges())
            {
                Microsoft.Msagl.Core.Layout.Node target = graph.FindNodeByUserData(edge.to);
                if (target is null)
                {
                    continue;
                }

                Microsoft.Msagl.Core.Layout.Edge e = new Microsoft.Msagl.Core.Layout.Edge(node, target);
                node.AddInEdge(e);
                target.AddOutEdge(e);
                graph.Edges.Add(e);
            }
        }

        //calc layout, apply positions to original TestNodeElements
        var settings = new Microsoft.Msagl.Layout.MDS.MdsLayoutSettings();
        settings.ScaleX = 800;
        settings.ScaleY = 800;
        settings.NodeSeparation = 60;
        Microsoft.Msagl.Miscellaneous.LayoutHelpers.CalculateLayout(graph, settings, null);

        // Move model to positive axis.
        graph.UpdateBoundingBox();
        graph.Translate(new Microsoft.Msagl.Core.Geometry.Point(-graph.Left, -graph.Bottom));

        // Update node position.
        foreach (var node in graph.Nodes)
        {
            Node graphViewNode = graphViewNodes.Find((b) => { return (b.userData as AIKit.SemanticWebNode) == (node.UserData as AIKit.SemanticWebNode); });
            Debug.Log("Moving node " + graphViewNode.name + " to (" + node.BoundingBox.Center.X + "," + node.BoundingBox.Center.X + ")");
            graphViewNode.SetPosition(new Rect((float)node.BoundingBox.Center.X, (float)node.BoundingBox.Center.Y, 10, 10));
        }


    }

    //Placeholder functions
    private GraphViewChange GraphViewChanged(GraphViewChange graphViewChange)
    {


        return graphViewChange;
    }


    private void KeyDown(KeyDownEvent evt)
    {

    }
}