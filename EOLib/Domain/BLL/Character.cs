// Original Work Copyright (c) Ethan Moffat 2014-2016
// This file is subject to the GPL v2 License
// For additional details, see the LICENSE file

using System.Collections.Generic;
using System.Linq;
using EOLib.Domain.Character;

namespace EOLib.Domain.BLL
{
	public class Character : ICharacter
	{
		public int ID { get; private set; }

		public string Name { get; private set; }

		public string Title { get; private set; }

		public string GuildName { get; private set; }

		public string GuildRank { get; private set; }

		public string GuildTag { get; private set; }

		public byte ClassID { get; private set; }

		public AdminLevel AdminLevel { get; private set; }

		public IReadOnlyList<short> Paperdoll { get; private set; }

		public ICharacterRenderProperties RenderProperties { get; private set; }

		public ICharacterStats Stats { get; private set; }

		public ICharacter WithID(int id)
		{
			var character = MakeCopy(this);
			character.ID = id;
			return character;
		}

		public ICharacter WithName(string name)
		{
			var character = MakeCopy(this);
			character.Name = name;
			return character;
		}

		public ICharacter WithTitle(string title)
		{
			var character = MakeCopy(this);
			character.Title = title;
			return character;
		}

		public ICharacter WithGuildName(string guildName)
		{
			var character = MakeCopy(this);
			character.GuildName = guildName;
			return character;
		}

		public ICharacter WithGuildRank(string guildRank)
		{
			var character = MakeCopy(this);
			character.GuildRank = guildRank;
			return character;
		}

		public ICharacter WithGuildTag(string guildTag)
		{
			var character = MakeCopy(this);
			character.GuildTag = guildTag;
			return character;
		}

		public ICharacter WithClassID(byte newClassID)
		{
			var character = MakeCopy(this);
			character.ClassID = newClassID;
			return character;
		}

		public ICharacter WithAdminLevel(AdminLevel level)
		{
			var character = MakeCopy(this);
			character.AdminLevel = level;
			return character;
		}

		public ICharacter WithPaperdoll(IEnumerable<short> paperdollItemIDs)
		{
			var character = MakeCopy(this);
			character.Paperdoll = paperdollItemIDs.ToList();
			return character;
		}

		public ICharacter WithRenderProperties(ICharacterRenderProperties renderProperties)
		{
			var character = MakeCopy(this);
			character.RenderProperties = renderProperties;
			return character;
		}

		public ICharacter WithStats(ICharacterStats stats)
		{
			var character = MakeCopy(this);
			character.Stats = stats;
			return character;
		}

		private static Character MakeCopy(ICharacter source)
		{
			return new Character
			{
				ID = source.ID,
				Name = source.Name,
				Title = source.Title,
				GuildName = source.GuildName,
				GuildRank = source.GuildRank,
				GuildTag = source.GuildTag,
				ClassID = source.ClassID,
				AdminLevel = source.AdminLevel,
				RenderProperties = source.RenderProperties,
				Stats = source.Stats,
				Paperdoll = source.Paperdoll
			};
		}
	}
}