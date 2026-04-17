// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Core;
using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;

namespace AdrPlus.Infrastructure.UI
{
    /// <summary>
    /// Interface for console output operations.
    /// Provides a testable abstraction over console writing.
    /// </summary>
    internal interface IConsoleWriter
    {
        /// <summary>
        /// Ensures that the console culture settings are properly configured based on the provided application configuration. 
        /// </summary>
        /// <param name="config">
        /// The application configuration containing culture settings to apply to the console. This may include settings such as language, date formats, and other culture-specific configurations that affect how information is displayed in the console.
        /// </param>
        void EnsureCulture(AdrPlusConfig config);

        /// <summary>
        /// Gets the current position of the cursor.
        /// </summary>
        /// <returns>A tuple containing the left and top positions of the cursor.</returns>
        (int left, int top) GetCursorPosition();

        void WriteWait(string message);

        /// <summary>
        /// Clears the wait message from the console at current positions and set positions of the cursor. 
        /// </summary>
        /// <param name="position">
        /// A tuple containing the left and top positions where the wait message was displayed, used to clear the message and reset the cursor position.
        /// </param>
        void ClearWait((int left, int top) position);

        /// <summary>
        /// Writes an informational message to the console.
        /// </summary>
        void WriteInfo(string message);

        /// <summary>
        /// Writes a message to the console indicating that an operation is being resumed before a wait or pause.
        /// </summary>
        void WriteSummary(string message);

        /// <summary>
        /// Writes a success message to the console.
        /// </summary>
        void WriteSuccess(string message);

        /// <summary>
        /// Writes an error message to the console.
        /// </summary>
        void WriteError(string message);

        /// <summary>
        /// Writes help information to the console.
        /// </summary>
        void WriteHelp(string helpText);

        /// <summary>
        /// Writes the specified command to the output stream.
        /// </summary>
        /// <param name="command">The command string to write.</param>
        void WriteStartCommand(string command);

        /// <summary>
        /// Writes a message indicating that the specified command has finished executing. 
        /// </summary>
        /// <param name="command">The command string to write.</param>
        void WriteFinishedCommand(string command);

        /// <summary>
        /// Displays a welcome message including the specified application version.
        /// </summary>
        /// <param name="appVersion">The version of the application to include in the welcome message.</param>
        void ShowWellcome(string appVersion);

        /// <summary>
        /// Configures the prompt settings. 
        /// </summary>
        /// <param name="config">The configuration settings to apply to the prompt.</param>
        void ConfigurePrompt(AdrPlusConfig config);

        /// <summary>
        /// Displays a banner with the specified text. 
        /// </summary>
        /// <param name="bannerText">The text to display in the banner.</param>
        void ShowBanner(string bannerText);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message">The message to display to the user.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A boolean indicating whether the user pressed abort key.</returns>
        bool PressAnyKeyToContinue(string message,CancellationToken cancellationToken);

        /// <summary>
        /// Enables or disables the ability for the user to abort an operation by pressing the Escape key during prompts. 
        /// </summary>
        /// <param name="enabled"> A boolean value indicating whether pressing the Escape key should abort the current operation. If set to true, the user can press Escape to cancel prompts; if false, the Escape key will not have any effect on prompt cancellation.
        /// </param>
        void EnabledEscToAbort(bool enabled);

        /// <summary>
        /// Prompts the user to select an option from a menu.
        /// </summary>
        /// <param name="IsHasconfig">Indicates whether configuration is already available to influence menu behavior.</param>
        /// <param name="itemMenus">The array of menu items to choose from.</param>
        /// <param name="defaultvalue">The default menu option selected when the prompt starts.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple indicating whether the operation was aborted and the user's selected menu item.</returns>  
        (bool IsAborted, ItemMenuWizard? Content) PromptSelectMenu(bool IsHasconfig, ItemMenuWizard[] itemMenus,ItemMenuWizard defaultvalue, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user for confirmation with a yes/no question. 
        /// </summary>
        /// <param name="message">The message to display to the user.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple indicating whether the operation was aborted and the user's response.</returns>
        (bool IsAborted, bool ConfirmYes) PromptConfirm(string message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to select an option from a list of fields.
        /// </summary>
        /// <param name="defaultvalue">The default value to select.</param>
        /// <param name="fields">The list of fields to choose from.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple indicating whether the operation was aborted and the user's selected field.</returns>
        (bool IsAborted, FieldsJson? Content) PromptConfigJsonRepoSelect(FieldsJson defaultvalue, IEnumerable<FieldsJson> fields, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to select an option from a list of fields.
        /// </summary>
        /// <param name="defaultvalue">The default value to select.</param>
        /// <param name="fields">The list of fields to choose from.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple indicating whether the operation was aborted and the user's selected field.</returns>
        (bool IsAborted, FieldsJson? Content) PromptConfigJsonAppSelect(FieldsJson defaultvalue, IEnumerable<FieldsJson> fields, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to edit a field prefix and returns the result along with an abort status.  
        /// </summary>
        /// <param name="fieldsJson">The fields metadata used to guide the prompt.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the entered content.</returns>
        (bool IsAborted, string Content) PromptEditFieldPrefix(FieldsJson fieldsJson, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to edit a field length sequence and returns the result along with an abort status. 
        /// </summary>
        /// <param name="fieldsJson"> The fields metadata used to guide the prompt.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the entered content.</returns>
        (bool IsAborted, int Content) PromptEditFieldLenSeq(FieldsJson fieldsJson, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to edit a field language and returns the result along with an abort status.
        /// </summary>
        /// <param name="fieldsJson">The fields metadata used to guide the prompt.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the entered language content.</returns>
        (bool IsAborted, string Content) PromptEditFieldLanguage(FieldsJson fieldsJson, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to edit the repository folder path and returns the result along with an abort status.
        /// </summary>
        /// <param name="fieldsJson">The fields metadata used to guide the prompt.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the entered folder path.</returns>
        (bool IsAborted, string Content) PromptEditFieldFolderRepo(FieldsJson fieldsJson, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to edit the command to open ADR files and returns the result along with an abort status.
        /// </summary>
        /// <param name="fieldsJson">The fields metadata used to guide the prompt.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the entered command content.</returns>
        (bool IsAborted, string Content) PromptEditFielOpenAdr(FieldsJson fieldsJson, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to edit the date format and returns the result along with an abort status.
        /// </summary>
        /// <param name="fieldsJson">The fields metadata used to guide the prompt.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the entered date format.</returns>
        (bool IsAborted, string Content) PromptEditFielDateFormat(FieldsJson fieldsJson, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to edit version field length and returns the result along with an abort status.
        /// </summary>
        /// <param name="fieldsJson">The fields metadata used to guide the prompt.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the entered field length.</returns>
        (bool IsAborted, int Content) PromptEditFieldVersion(FieldsJson fieldsJson, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to edit revision field length and returns the result along with an abort status.
        /// </summary>
        /// <param name="fieldsJson">The fields metadata used to guide the prompt.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the entered field length.</returns>
        (bool IsAborted, int Content) PromptEditFieldRevision(FieldsJson fieldsJson, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to edit the available scopes and returns the result along with an abort status.
        /// </summary>
        /// <param name="fieldsJson">The fields metadata used to guide the prompt.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the entered scopes content.</returns>
        (bool IsAborted, string Content) PromptEditFieldScopes(FieldsJson fieldsJson, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to edit the skip detail field and returns the result along with an abort status.
        /// </summary>
        /// <param name="fieldsJson">The fields metadata used to guide the prompt.</param>
        /// <param name="fields">The available field options to choose from.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the entered skip detail content.</returns>
        (bool IsAborted, string Content) PromptEditFieldskipdomain(FieldsJson fieldsJson, IEnumerable<FieldsJson> fields, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to edit a yes/no character field and returns the result along with an abort status.
        /// </summary>
        /// <param name="fieldsJson">The fields metadata used to guide the prompt.</param>
        /// <param name="fields">The available field options to choose from.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the entered character content.</returns>
        (bool IsAborted, string Content) PromptEditFieldYesNoChar(FieldsJson fieldsJson, IEnumerable<FieldsJson> fields, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to edit the scope field length and returns the result along with an abort status.
        /// </summary>
        /// <param name="fieldsJson">The fields metadata used to guide the prompt.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the entered scope length.</returns>
        (bool IsAborted, int Content) PromptEditFieldLenScope(FieldsJson fieldsJson, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to edit the folder-by-scope setting and returns the result along with an abort status.
        /// </summary>
        /// <param name="fieldsJson">The fields metadata used to guide the prompt.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the selected boolean value.</returns>
        (bool IsAborted, bool Content) PromptEditFieldFolderByScope(FieldsJson fieldsJson, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to edit the case transformation format and returns the result along with an abort status.
        /// </summary>
        /// <param name="fieldsJson">The fields metadata used to guide the prompt.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the entered case format.</returns>
        (bool IsAborted, string Content) PromptEditFieldCaseTransform(FieldsJson fieldsJson, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to edit the field separator character and returns the result along with an abort status.
        /// </summary>
        /// <param name="fieldsJson">The fields metadata used to guide the prompt.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the entered separator.</returns>
        (bool IsAborted, string Content) PromptEditFieldSeparator(FieldsJson fieldsJson, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to edit a status field value and returns the result along with an abort status.
        /// </summary>
        /// <param name="fieldsJson">The fields metadata used to guide the prompt.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the entered status.</returns>
        (bool IsAborted, string Content) PromptEditFieldStatus(FieldsJson fieldsJson, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to edit the header disclaimer text and returns the result along with an abort status.
        /// </summary>
        /// <param name="fieldsJson">The fields metadata used to guide the prompt.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the entered disclaimer text.</returns>
        (bool IsAborted, string Content) PromptEditFieldHeaderDisclaimer(FieldsJson fieldsJson, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to edit the header status label and returns the result along with an abort status.
        /// </summary>
        /// <param name="fieldsJson">The fields metadata used to guide the prompt.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the entered status label.</returns>
        (bool IsAborted, string Content) PromptEditFieldHeaderStatus(FieldsJson fieldsJson, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to edit the header version label and returns the result along with an abort status.
        /// </summary>
        /// <param name="fieldsJson">The fields metadata used to guide the prompt.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the entered version label.</returns>
        (bool IsAborted, string Content) PromptEditFieldHeaderVersion(FieldsJson fieldsJson, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to edit the header revision label and returns the result along with an abort status.
        /// </summary>
        /// <param name="fieldsJson">The fields metadata used to guide the prompt.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the entered revision label.</returns>
        (bool IsAborted, string Content) PromptEditFieldHeaderRevision(FieldsJson fieldsJson, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to select a logical drive from available drives.
        /// </summary>
        /// <param name="message">The message to display to the user.</param>
        /// <param name="fileSystemService">The file system service to enumerate available drives.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the selected drive path.</returns>
        (bool IsAborted, string Content) PromptSelectLogicalDrive(string message, IFileSystemService fileSystemService, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to select or create a folder for the ADR repository.
        /// </summary>
        /// <param name="checknitCmd">Whether to check for init command requirements.</param>
        /// <param name="root">The root directory path to start browsing from.</param>
        /// <param name="fileSystemService">The file system service to use for directory operations.</param>
        /// <param name="validateJsonConfig">The service to validate JSON configuration.</param>
        /// <param name="repoConfig">The repository configuration.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the selected folder path.</returns>
        (bool IsAborted, string Content) PromptSelectFolderRepositoryAdr(bool checknitCmd, string root, IFileSystemService fileSystemService, IValidateJsonConfig validateJsonConfig, AdrPlusConfig repoConfig, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to edit the title of an ADR.
        /// </summary>
        /// <param name="defaultTitle">The default title to display.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the entered title.</returns>
        (bool IsAborted, string Content) PromptEditTitleAdr(string defaultTitle, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to edit the scope of an ADR.
        /// </summary>
        /// <param name="defaultScope">The default scope to display.</param>
        /// <param name="adrPlusRepo">The repository configuration containing available scopes.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the entered scope.</returns>
        (bool IsAborted, string Content) PromptEditScopeAdr(string defaultScope, AdrPlusRepoConfig adrPlusRepo, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts to retrieve the array of available domains from existing ADR files.
        /// </summary>
        /// <param name="fileSystemService">The file system service to use for file operations.</param>
        /// <param name="path">The directory path to search for ADR files.</param>
        /// <param name="config">The application configuration.</param>
        /// <param name="adrPlusRepo">The repository configuration.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing abort status, array of domains, and any exception that occurred.</returns>
        (bool IsAborted, string[] domains, Exception? Content) PromptGetArrayDomainsAdr(IFileSystemService fileSystemService, string path, AdrPlusConfig config, AdrPlusRepoConfig adrPlusRepo, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to edit the domain of an ADR with suggested domains.
        /// </summary>
        /// <param name="defaultdomain">The default domain to display.</param>
        /// <param name="sugestdomains">An array of suggested domains to choose from.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the entered domain.</returns>
        (bool IsAborted, string Content) PromptEditDomainAdr(string defaultdomain, string[] sugestdomains, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to select a date using a calendar interface.
        /// </summary>
        /// <param name="message">The message to display to the user.</param>
        /// <param name="dateref">The reference date to display initially.</param>
        /// <param name="adrPlusRepo">The repository configuration.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the selected date.</returns>
        (bool IsAborted, DateTime Content) PrompCalendar(string message, DateTime dateref, AdrPlusConfig adrPlusRepo, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to select an ADR from a list of latest ADR files.
        /// </summary>
        /// <param name="files">The array of ADR files to choose from.</param>
        /// <param name="adrPlusRepoConfig">The repository configuration.</param>
        /// <param name="validselect">Validation function that checks whether a selected ADR is valid and returns a message when invalid.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the selected ADR information.</returns>
        (bool IsAborted, AdrFileNameComponents? info) PromptSelecLatesAdrs(AdrFileNameComponents[] files, AdrPlusRepoConfig adrPlusRepoConfig, Func<AdrFileNameComponents, (bool, string?)> validselect, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to select an ADR template file for configuration. 
        /// </summary>
        /// <param name="root">The root directory where template discovery starts.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns> 
        /// A tuple containing a boolean indicating if the operation was aborted and the file path of the selected ADR template. If the operation was aborted, the file path will be null or empty.
        /// </returns>
        (bool IsAborted, string FilePathAdrTemplate) PromptConfigTemplateAdrSelect(string root, CancellationToken cancellationToken = default);
    }
}
