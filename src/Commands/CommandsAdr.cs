// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Commands.Approve;
using AdrPlus.Commands.Config;
using AdrPlus.Commands.Help;
using AdrPlus.Commands.Init;
using AdrPlus.Commands.NewAdr;
using AdrPlus.Commands.Reject;
using AdrPlus.Commands.Repo;
using AdrPlus.Commands.Review;
using AdrPlus.Commands.Supersede;
using AdrPlus.Commands.UndoStatus;
using AdrPlus.Commands.Version;
using AdrPlus.Commands.Wizard;

namespace AdrPlus.Commands
{
    /// <summary>
    /// Enumeration of available commands for the ADR management application, each associated with a command handler and description. 
    /// </summary>
    internal enum CommandsAdr
    {
        /// <summary>
        /// Displays help information for the available commands.
        /// </summary>
        [Command("help", typeof(HelpCommandHandler), "CmdDescHelp")]
        Help,
        /// <summary>
        /// Launches the wizard for guided operations.
        /// </summary>
        [Command("wizard", typeof(WizardCommandHandler), "CmdDescWizard")]
        Wizard,
        /// <summary>
        /// Launches configuration editor for the application and repository settings.
        /// </summary>
        [Command("config", typeof(ConfigCommandHandler), "CmdDescConfig")]
        Config,
        /// <summary>
        /// Initializes the repository with folders for ADRs.
        /// </summary>
        [Command("init", typeof(InitCommandHandler), "CmdDescInit")]
        Init,
        /// <summary>
        /// Upgrade repository's settings.
        /// </summary>
        [Command("repo", typeof(RepoCommandHandler), "CmdDescRepo")]
        Repo,
        /// <summary>
        /// Creates a new ADR with a new number (incremental number).
        /// </summary>
        [Command("new", typeof(NewAdrCommandHandler), "CmdDescNew")]
        New,
        /// <summary>
        /// Creates a new ADR with a new version (incremental version, same number).
        /// </summary>
        [Command("version", typeof(VersionCommandHandler), "CmdDescVersion")]
        Version,
        /// <summary>
        /// Creates a new ADR with a new revision (incremental revision, same number and version).
        /// </summary>
        [Command("review", typeof(ReviewCommandHandler), "CmdDescReview")]
        Review,
        /// <summary>
        /// Supersedes an ADR (incremental number, reset revision and version).
        /// </summary>
        [Command("supersede", typeof(SupersedeCommandHandler), "CmdDescSupersede")]
        Supersede,
        /// <summary>
        /// Updates the status of an ADR to accepted
        /// </summary>
        [Command("approve", typeof(ApproveCommandHandler), "CmdDescAccepted")]
        Approve,
        /// <summary>
        /// Updates the status of an ADR to rejected
        /// </summary>
        [Command("reject", typeof(RejectCommandHandler), "CmdDescRejected")]
        Reject,
        /// <summary>
        /// Updates the status of an ADR to rejected
        /// </summary>
        [Command("undo", typeof(UndoStatusCommandHandler), "CmdDescUndoStatus")]
        UndoStatus
    }
}
