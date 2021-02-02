using System;
using System.Collections.Generic;
using System.Text;
using YamlDotNet.Serialization;

namespace AdvocatesListService.Model
{
    public class Advocate
    {
        public string Name { get; set; }
        public Metadata Metadata { get; set; }
        
        [YamlMember(Alias = "connect")]
        public List<Connect> Connects { get; set; } //connect
    }

    public class Metadata
    {
        public string Team { get; set; }
        [YamlMember(Alias = "ms.author")]
        public string Alias { get; set; } //ms.author
    }

    public class Connect
    {
        public string Title { get; set; }
        public string Url { get; set; }
    }
}
