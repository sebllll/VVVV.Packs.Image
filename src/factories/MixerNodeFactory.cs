using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SlimDX.Direct3D10;
using VVVV.Hosting.Factories;
using VVVV.Hosting.Graph;
using VVVV.Hosting.Interfaces;
using VVVV.Hosting.IO;
using VVVV.CV.Core;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.Collections;

namespace VVVV.CV.Factories
{
    [Export(typeof(IAddonFactory))]
    public class MixerNodeFactory : AbstractFileFactory<IInternalPluginHost>
    {
#pragma warning disable 0649
        [Import]
        private IORegistry FIORegistry;

        [Import]
        private DotNetPluginFactory FPluginFactory;
#pragma warning restore

        private readonly CompositionContainer FParentContainer;
        private Type FReflectionOnlyIMixerInstanceType;

        [ImportingConstructor]
        public MixerNodeFactory(CompositionContainer parentContainer)
            : base(".dll")
        {
            FParentContainer = parentContainer;
        }

        protected override bool CreateNode(INodeInfo nodeInfo, IInternalPluginHost nodeHost)
        {
            var assemblyLocation = nodeInfo.Filename;
            var assembly = Assembly.LoadFrom(assemblyLocation);
            var mixerInstanceType = assembly.GetType(nodeInfo.Arguments);
            var genericMixerNodeType = typeof(MixerNode<>);
            var mixerNodeType = genericMixerNodeType.MakeGenericType(mixerInstanceType);
            var pluginContainer = new PluginContainer(nodeHost, FIORegistry, FParentContainer, FNodeInfoFactory, FPluginFactory, mixerNodeType, nodeInfo);
            nodeHost.Plugin = pluginContainer;
            return true;
        }

        protected override bool DeleteNode(INodeInfo nodeInfo, IInternalPluginHost nodeHost)
        {
            var pluginContainer = nodeHost.Plugin as PluginContainer;
            pluginContainer.Dispose();
            return true;
        }

        public override string JobStdSubPath
        {
            get { return "plugins"; }
        }

        protected override IEnumerable<INodeInfo> LoadNodeInfos(string filename)
        {
            if (!IsDotNetAssembly(filename)) yield break;
            if (FReflectionOnlyIMixerInstanceType == null)
            {
                // Can't get it to load in constructor
                var cvCoreAssemblyName = typeof(IMixerInstance).Assembly.FullName;
                var cvCoreAssembly = Assembly.ReflectionOnlyLoad(cvCoreAssemblyName);
                FReflectionOnlyIMixerInstanceType = cvCoreAssembly.GetExportedTypes()
                    .Where(t => t.Name == typeof(IMixerInstance).Name)
                    .First();
            }

            var assembly = Assembly.ReflectionOnlyLoadFrom(filename);
            foreach (var type in assembly.GetExportedTypes())
            {
                if (!type.IsAbstract && !type.IsGenericTypeDefinition && FReflectionOnlyIMixerInstanceType.IsAssignableFrom(type))
                {
                    var attribute = GetMixerInstanceAttributeData(type);

                    if (attribute != null)
                    {
                        var nodeInfo = ExtractNodeInfoFromAttributeData(attribute, filename);
                        nodeInfo.Arguments = type.FullName;
                        nodeInfo.Type = NodeType.Plugin;
                        nodeInfo.Factory = this;
                        nodeInfo.CommitUpdate();
                        yield return nodeInfo;
                    }
                }
            }
        }

        private static CustomAttributeData GetMixerInstanceAttributeData(Type type)
        {
            return CustomAttributeData.GetCustomAttributes(type)
                .Where(ca => ca.Constructor.DeclaringType.FullName == typeof(MixerInstanceAttribute).FullName)
                .FirstOrDefault();
        }

        private INodeInfo ExtractNodeInfoFromAttributeData(CustomAttributeData attribute, string filename)
        {
            var name = attribute.ConstructorArguments[0].Value as string;
            //var namedArguments = new Dictionary<string, object>();
            //foreach (var namedArgument in attribute.NamedArguments)
            //{
            //    namedArguments[namedArgument.MemberInfo.Name] = namedArgument.TypedValue.Value;
            //}

            var attributes = new Dictionary<string, string>();

            var namedArguments = attribute.NamedArguments;

            foreach (var argument in namedArguments)
            {
                var argumentName = argument.MemberInfo.Name;
                var value = argument.TypedValue.Value.ToString();

                attributes.Add(argumentName, value);
            }

            string version;
            attributes.TryGetValue("Version", out version);

            var nodeInfo = FNodeInfoFactory.CreateNodeInfo(
                name,
                "CV.Image",
                version,
                filename,
                true);

            string author;
            attributes.TryGetValue("Author", out author);
            nodeInfo.Author = author;

            string help;
            attributes.TryGetValue("Help", out help);
            nodeInfo.Help = help;

            string credits;
            attributes.TryGetValue("Credits", out credits);
            nodeInfo.Credits = credits;

            string tags;
            attributes.TryGetValue("Tags", out tags);
            nodeInfo.Tags = tags;

            //foreach (var entry in namedArguments)
            //{
            //    nodeInfo.GetType().InvokeMember((string)entry.Key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty, Type.DefaultBinder, nodeInfo, new object[] { entry.Value });
            //}

            return nodeInfo;
        }

        // TODO: Should be a utility function in VVVV.Utils
        // From http://www.anastasiosyal.com/archive/2007/04/17/3.aspx
        private static bool IsDotNetAssembly(string fileName)
        {
            using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                try
                {
                    using (var binReader = new BinaryReader(fs))
                    {
                        try
                        {
                            fs.Position = 0x3C; //PE Header start offset
                            uint headerOffset = binReader.ReadUInt32();

                            fs.Position = headerOffset + 0x18;
                            UInt16 magicNumber = binReader.ReadUInt16();

                            int dictionaryOffset;
                            switch (magicNumber)
                            {
                                case 0x010B: dictionaryOffset = 0x60; break;
                                case 0x020B: dictionaryOffset = 0x70; break;
                                default:
                                    throw new BadImageFormatException("Invalid Image Format");
                            }

                            //position to RVA 15
                            fs.Position = headerOffset + 0x18 + dictionaryOffset + 0x70;


                            //Read the value
                            uint rva15value = binReader.ReadUInt32();
                            return rva15value != 0;
                        }
                        finally
                        {
                            binReader.Close();
                        }
                    }
                }
                finally
                {
                    fs.Close();
                }

            }
        }
    }
}
