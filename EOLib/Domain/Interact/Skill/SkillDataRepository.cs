﻿using AutomaticTypeMapper;
using System.Collections.Generic;

namespace EOLib.Domain.Interact.Skill
{
    public interface ISkillDataRepository : IResettable
    {
        short ID { get; set; }

        string Title { get; set; }

        HashSet<Skill> Skills { get; set; }
    }

    public interface ISkillDataProvider : IResettable
    {
        short ID { get; }

        string Title { get; }

        IReadOnlyCollection<Skill> Skills { get; }
    }

    [AutoMappedType(IsSingleton = true)]
    public class SkillDataRepository : ISkillDataRepository, ISkillDataProvider
    {
        public short ID { get; set; }

        public string Title { get; set; }

        public HashSet<Skill> Skills { get; set; }

        IReadOnlyCollection<Skill> ISkillDataProvider.Skills => Skills;

        public SkillDataRepository()
        {
            ResetState();
        }

        public void ResetState()
        {
            ID = 0;
            Title = string.Empty;
            Skills = new HashSet<Skill>();
        }
    }
}
