using Autodesk.Revit.DB;
using Org.BouncyCastle.Tls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoloBlok.Utils.Parameters
{
    internal static class HBProjectParameterUtils
    {
        private const string tempSharedGroupName = "TemporaryParameters";

        public static Definition CreateLinkedIdParameter(Document doc, string paramName)
        {
            var app = doc.Application;

            var bindingMap = doc.ParameterBindings;

            // Check if parameter already exists
            bool parameterExists = DoesParameterExist(bindingMap, paramName);
            if (parameterExists)
            {
                return GetParameterDefinitionByName(bindingMap, paramName);
            }

            // Open the shared parameter file
            var sharedParamFile = app.OpenSharedParameterFile();
            if (sharedParamFile == null)
                throw new InvalidOperationException("Shared paramter file is not set. Set it in Revit before running this tool.");

            // Create or get the group
            DefinitionGroup group = sharedParamFile.Groups.get_Item(tempSharedGroupName) ??
                                    sharedParamFile.Groups.Create(tempSharedGroupName);

            // Create the parameter definition
            Definition definition = group.Definitions.get_Item(paramName);
            if (definition == null)
            {
                var definitionOptions = new ExternalDefinitionCreationOptions(paramName, SpecTypeId.String.Text);
                definition = group.Definitions.Create(definitionOptions);
            }

            // Create a category set and add the desired categories
            CategorySet categorySet = GetCategorySet(doc, bindingMap, BuiltInCategory.OST_DataDevices);

            // Create an instance binding
            var instanceBinding = app.Create.NewInstanceBinding(categorySet);

            // Add to the document's bindings
            bindingMap.Insert(definition, instanceBinding, GroupTypeId.IdentityData);

            return definition;
        }


        private static CategorySet GetCategorySet(Document doc, BindingMap bindingMap, BuiltInCategory category)
        {
            var categorySet = doc.Application.Create.NewCategorySet();
            categorySet.Insert(doc.Settings.Categories.get_Item(category));

            return categorySet;
        }


        private static Definition GetParameterDefinitionByName(BindingMap bindingMap, string paramName)
        {
            DefinitionBindingMapIterator iterator = bindingMap.ForwardIterator();
            while (iterator.MoveNext())
            {
                Definition definition = iterator.Key;
                if (definition.Name.Equals(paramName, StringComparison.OrdinalIgnoreCase))
                {
                    return definition;
                }
            }

            return null; // Parameter not found
        }

        private static bool DoesParameterExist(BindingMap bindingMap, string paramName)
        {
            return GetParameterDefinitionByName(bindingMap, paramName) != null;
        }
    }
}
