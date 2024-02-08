using PluginBase;
using System;
using System.Windows.Input;

namespace HelloPlugin
{
    public class HelloCommand : PluginBase.ICommand
    {
        public string Name { get => "hello"; }
        public string Description { get => "Displays hello message."; }

        public int Execute()
        {
            Console.WriteLine("Hello !!!");
            return 0;
        }
    }
}