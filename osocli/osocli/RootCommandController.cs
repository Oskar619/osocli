﻿using System;
using System.CommandLine;
using System.Linq;
using System.Reflection;

namespace osocli
{
    public class RootCommandController : RootCommand
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="RootCommandController"/> class.
        /// Root Command holder for IntelliCode CLI.
        /// </summary>
        /// <param name="children">The children command list.</param>
        public RootCommandController(string description)
            : base(description)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RootCommandController"/> class.
        /// Root Command holder for IntelliCode CLI.
        /// </summary>
        /// <param name="children">The children command list.</param>
        public RootCommandController(string description, params Command[] children)
            : base(description)
        {
            foreach (var command in children)
            {
                AddCommand(command);
            }
        }

        public void Init()
        {
            var types = Assembly.GetCallingAssembly().GetTypes();
            // Get commands using reflection.
            var commands = Assembly.GetCallingAssembly()
                                   .GetTypes()
                                   .Where(t => t.IsClass && typeof(BaseCommand).IsAssignableFrom(t) && t.GetConstructor(Type.EmptyTypes) != null)
                                   .Select(t => Activator.CreateInstance(t) as BaseCommand)
                                   .ToList();

            // Add the commands.
            foreach(var command in commands)
            {
                AddCommand(command);
            }
        }
    }
}
