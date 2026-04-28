// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

namespace AdrPlus.Commands
{
    /// <summary>
    /// Enumeration of command-line arguments supported by AdrPlus commands.
    /// </summary>
    internal enum Arguments
    {
        [CommandArgument("-w", "--wizard")]
        [HelpUsage(UsageArgumments.Wizard, "HelpUsageWizardNew")]
        WizardNew,
        [CommandArgument("-w", "--wizard")]
        [HelpUsage(UsageArgumments.Wizard, "HelpUsageWizardNewVer")]
        WizardVersion,
        [CommandArgument("-w", "--wizard")]
        [HelpUsage(UsageArgumments.Wizard, "HelpUsageWizardNewRev")]
        WizardReview,
        [CommandArgument("-w", "--wizard")]
        [HelpUsage(UsageArgumments.Wizard, "HelpUsageWizardSupersede")]
        WizardSupersede,
        [CommandArgument("-w", "--wizard")]
        [HelpUsage(UsageArgumments.Wizard, "HelpUsageWizardApprove")]
        WizardApprove,
        [CommandArgument("-w", "--wizard")]
        [HelpUsage(UsageArgumments.Wizard, "HelpUsageWizardReject")]
        WizardReject,
        [CommandArgument("-w", "--wizard")]
        [HelpUsage(UsageArgumments.Wizard, "HelpUsageWizardUndoStatus")]
        WizardUndoStatus,
        [CommandArgument("-w", "--wizard")]
        [HelpUsage(UsageArgumments.Wizard, "HelpUsageWizardInit")]
        WizardInit,
        [CommandArgument("-w", "--wizard")]
        [HelpUsage(UsageArgumments.Wizard, "HelpUsageWizardRepo")]
        WizardRepo,
        [CommandArgument("-a", "--application")]
        [HelpUsage(UsageArgumments.Wizard, "HelpUsageWizardConfigApp")]
        WizardConfigApplication,
        [CommandArgument("-t", "--template")]
        [HelpUsage(UsageArgumments.Wizard, "HelpUsageWizardConfigTemplate")]
        WizardConfigTemplate,
        [CommandArgument("-r", "--repository")]
        [HelpUsage(UsageArgumments.Wizard, "HelpUsageWizardConfigRepo")]
        WizardConfigRepository,
        [CommandArgument("-f", "--file")]
        [HelpUsage(UsageArgumments.OptionalWithValue, "HelpUsageFileConfig")]
        FileConfig,
        [CommandArgument("-f", "--file")]
        [HelpUsage(UsageArgumments.OptionalWithValueWhenWizard, "HelpUsageFileAdr")]
        FileAdr,
        [CommandArgument("-s", "--sequence")]
        [HelpUsage(UsageArgumments.OptionalWithValueWhenWizard, "HelpUsageSequenceAdr")]
        SequenceAdr,
        [CommandArgument("-p", "--path")]
        [HelpUsage(UsageArgumments.OptionalWithValueWhenWizard, "HelpUsageTargetRepoPath")]
        TargetRepo,
        [CommandArgument("-p", "--path")]
        [HelpUsage(UsageArgumments.OptionalWithValueWhenWizard, "HelpUsageTargetRepoAdrPath")]
        TargetRepoAdr,
        [CommandArgument("-t", "--title")]
        [HelpUsage(UsageArgumments.OptionalWithValueWhenWizard, "HelpUsageTitleAdr")]
        TitleAdr,
        [CommandArgument("-d", "--domain")]
        [HelpUsage(UsageArgumments.OptionalWithValueWhenWizard, "HelpUsageDomainAdr")]
        DomainAdr,
        [CommandArgument("-s", "--scope")]
        [HelpUsage(UsageArgumments.OptionalWithValueWhenWizard, "HelpUsageScopeAdr")]
        ScopeAdr,
        [CommandArgument("-o", "--open")]
        [HelpUsage(UsageArgumments.Optional, "HelpUsageOpenAdr")]
        OpenAdr,
        [CommandArgument("-e", "--empty")]
        [HelpUsage(UsageArgumments.Optional, "HelpUsageEmptyAdr")]
        EmptyAdr,
        [CommandArgument("-r", "--refdate")]
        [HelpUsage(UsageArgumments.Optional, "HelpUsageDateRefAdr")]
        DateRefAdr,
        [CommandArgument("-t", "--template")]
        [HelpUsage(UsageArgumments.Optional, "HelpUsageRepoTemplate")]
        RepoTemplate,
        [CommandArgument("-v", "--version")]
        [HelpUsage(UsageArgumments.OptionalWithValue, "HelpUsageRepoVersion")]
        RepoVersion,
        [CommandArgument("-r", "--revision")]
        [HelpUsage(UsageArgumments.OptionalWithValue, "HelpUsageRepoRevision")]
        RepoRevision,
        [CommandArgument("-s", "--scope")]
        [HelpUsage(UsageArgumments.OptionalWithValue, "HelpUsageRepoScope")]
        RepoScope,
        [CommandArgument("-i", "--items")]
        [HelpUsage(UsageArgumments.OptionalWithValue, "HelpUsageRepoScopeItems")]
        RepoScopeItems,
        [CommandArgument("-c", "--createfolders")]
        [HelpUsage(UsageArgumments.Optional, "HelpUsageRepoWithFolders")]
        RepoWithFolders,
        [CommandArgument("-f", "--file")]
        [HelpUsage(UsageArgumments.OptionalWithValue, "HelpUsageFileTemplate")]
        FileTemplate,
        [CommandArgument("-h", "--help")]
        [HelpUsage(UsageArgumments.Optional, "HelpUsageHelp")]
        Help,
    }
}
