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

        Button contentsButton = new Button(() => { Debug.Log("Clicked!"); });
        contentsButton.text = contentsButton.name = node.ToString();
        contents.Add(contentsButton);
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
        wnd.titleContent = new GUIContent("Test Graph Window");
        wnd.graphObject = Selection.activeGameObject.GetComponent<BeAnEntity>().GetSelf().knowledgeModule.lexicalMemory;
        Debug.Log("Oopening knowledge window for " + Selection.activeGameObject.GetComponent<BeAnEntity>().EntityName);
        wnd.CreateElements();
    }

    AIKit.LexicalMemory graphObject;

    private void CreateElements()
    {
        rootVisualElement.Clear();

        //if (graphObject.graphs.Length == 0) { graphObject.graphs = new TestGraph[1]; graphObject.graphs[0] = new TestGraph(); }

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
        int n = 0;
        List<(TestNodeElement, AIKit.SemanticWebNode)> graphViewNodes = new List<(TestNodeElement, AIKit.SemanticWebNode)>();

        foreach (AIKit.SemanticWebNode eNode in graphObject.GetAllNodes())
        {
            Debug.Log("Loading node " + (n++) + "...");
            TestNodeElement exampleNode = new TestNodeElement(eNode) { name = eNode.GetAliases()[0].ToString() };

            var port = exampleNode.InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
            port.portName = "Out";
            exampleNode.inputContainer.Add(port);

            var port2 = exampleNode.InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
            port2.portName = "In";
            exampleNode.outputContainer.Add(port2);

            exampleNode.RefreshExpandedState();
            exampleNode.RefreshPorts();

            graphView.AddElement(exampleNode);
            exampleNode.InitializeNode();
            rootVisualElement.MarkDirtyRepaint();

            graphViewNodes.Add((exampleNode, eNode));
        }

        //add edges
        foreach (var bundle in graphViewNodes)
        {
            foreach (AIKit.SemanticWebEdge edge in bundle.Item2.GetEdges())
            {
                Debug.Log(bundle.Item1.outputContainer.childCount);
                Port outPort = bundle.Item1.outputContainer[0] as Port;
                Port inPort = graphViewNodes.First<(TestNodeElement, AIKit.SemanticWebNode)>((b) => { return b.Item2 == edge.to; }).Item1.inputContainer[0] as Port;
                Edge e = outPort.ConnectTo(inPort);
                e.name = edge.word.ToString();
                graphView.AddElement(e);
            }
            
        }

        //layout using MSAGL
        ApplyMSAGLPosition(graphViewNodes);
    }

    private static void ApplyMSAGLPosition(List<(TestNodeElement, AIKit.SemanticWebNode)> graphViewNodes)
    {
        Microsoft.Msagl.Core.Layout.GeometryGraph graph = new Microsoft.Msagl.Core.Layout.GeometryGraph();

        //store semanticwebnode + msagl node
        List<(Microsoft.Msagl.Core.Layout.Node, AIKit.SemanticWebNode)> nodePairs = new List<(Microsoft.Msagl.Core.Layout.Node, AIKit.SemanticWebNode)>();

        //place all nodes
        int n = 0;
        foreach (var bundle in graphViewNodes)
        {
            Debug.Log("MSAGL Loading node " + (n++) + "...");
            Microsoft.Msagl.Core.Layout.Node msNode = new Microsoft.Msagl.Core.Layout.Node();
            msNode.UserData = bundle.Item2;
            graph.Nodes.Add(msNode);
            nodePairs.Add((msNode, bundle.Item2));
        }

        //add edges
        foreach (var node in graph.Nodes)
        {
            foreach (AIKit.SemanticWebEdge edge in (node.UserData as AIKit.SemanticWebNode).GetEdges())
            {
                Microsoft.Msagl.Core.Layout.Node target = graph.FindNodeByUserData(edge.to);
                Microsoft.Msagl.Core.Layout.Edge e = new Microsoft.Msagl.Core.Layout.Edge(node, target);
                node.AddOutEdge(e);
                target.AddInEdge(e);
                graph.Edges.Add(e);
            }
        }

        //calc layout, apply positions to original TestNodeElements
        var settings = new Microsoft.Msagl.Prototype.Ranking.RankingLayoutSettings();
        Microsoft.Msagl.Miscellaneous.LayoutHelpers.CalculateLayout(graph, settings, null);

        // Move model to positive axis.
        graph.UpdateBoundingBox();
        graph.Translate(new Microsoft.Msagl.Core.Geometry.Point(-graph.Left, -graph.Bottom));

        // Update node position.
        foreach (var node in graph.Nodes)
        {
            Node graphViewNode = graphViewNodes.Find((b) => { return b.Item2 == (node.UserData as AIKit.SemanticWebNode); }).Item1;
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