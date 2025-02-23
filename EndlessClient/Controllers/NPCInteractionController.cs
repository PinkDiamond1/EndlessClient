﻿using AutomaticTypeMapper;
using EndlessClient.Dialogs;
using EndlessClient.Dialogs.Actions;
using EndlessClient.HUD;
using EndlessClient.Input;
using EOLib.Domain.Interact;
using EOLib.Domain.NPC;
using EOLib.IO.Repositories;
using EOLib.Localization;
using System.Linq;

namespace EndlessClient.Controllers
{
    [AutoMappedType]
    public class NPCInteractionController : INPCInteractionController
    {
        private readonly IMapNPCActions _mapNpcActions;
        private readonly IInGameDialogActions _inGameDialogActions;
        private readonly IStatusLabelSetter _statusLabelSetter;
        private readonly IENFFileProvider _enfFileProvider;
        private readonly IActiveDialogProvider _activeDialogProvider;
        private readonly IUserInputRepository _userInputRepository;

        public NPCInteractionController(IMapNPCActions mapNpcActions,
                                        IInGameDialogActions inGameDialogActions,
                                        IStatusLabelSetter statusLabelSetter,
                                        IENFFileProvider enfFileProvider,
                                        IActiveDialogProvider activeDialogProvider,
                                        IUserInputRepository userInputRepository)
        {
            _mapNpcActions = mapNpcActions;
            _inGameDialogActions = inGameDialogActions;
            _statusLabelSetter = statusLabelSetter;
            _enfFileProvider = enfFileProvider;
            _activeDialogProvider = activeDialogProvider;
            _userInputRepository = userInputRepository;
        }

        public void ShowNPCDialog(NPC npc)
        {
            if (_activeDialogProvider.ActiveDialogs.Any(x => x.HasValue))
                return;

            var data = _enfFileProvider.ENFFile[npc.ID];

            // there is no "NPC" text in the localized files
            _statusLabelSetter.SetStatusLabel($"[ NPC ] {data.Name}");

            switch(data.Type)
            {
                case EOLib.IO.NPCType.Shop:
                    _mapNpcActions.RequestShop(npc);
                    _userInputRepository.ClickHandled = true;
                    break;
                case EOLib.IO.NPCType.Quest:
                    _mapNpcActions.RequestQuest(npc);
                    _userInputRepository.ClickHandled = true;
                    break;
                case EOLib.IO.NPCType.Bank:
                    _mapNpcActions.RequestBank(npc);
                    // note: the npc action types rely on a server response to show the dialog because they are driven
                    //       by config data on the server. Bank account dialog does not have this restriction;
                    //       interaction with the NPC should *always* show the dialog
                    _inGameDialogActions.ShowBankAccountDialog();
                    _userInputRepository.ClickHandled = true;
                    break;
                case EOLib.IO.NPCType.Skills:
                    _mapNpcActions.RequestSkillmaster(npc);
                    _userInputRepository.ClickHandled = true;
                    break;
            }
        }
    }

    public interface INPCInteractionController
    {
        void ShowNPCDialog(NPC npc);
    }
}
