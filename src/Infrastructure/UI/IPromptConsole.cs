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
    internal interface IPromptConsole
    {
       /// <summary>
       /// Attempts to execute the first-time installation process. 
       /// </summary>
       /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
       /// <returns>A task that represents the asynchronous operation. The task result contains <see langword="true"/> if the
       /// installation was executed successfully; otherwise, <see langword="false"/>.</returns>
        Task<bool> TryExecuteFistInstall(CancellationToken cancellationToken);

        /// <summary>
        /// Clears the console history related to migration operations, ensuring that any previous migration logs or messages are removed from the console output. This method is typically used to maintain a clean and organized console display during migration processes, allowing users to focus on current migration activities without being distracted by past logs. 
        /// </summary>
        void ClearHistoryMigration();


        /// <summary>
        /// Prompts the user to select a Title position for a file.
        /// </summary>
        /// <param name="filename">The name of the file.</param>
        /// <param name="maxValue">The maximum position value allowed.</param>
        /// <param name="defaultValue">The default position value.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A tuple containing whether the operation was aborted and the selected position value.</returns>
        (bool IsAborted, int Value) PromptSelectTitlePosition(string filename, int maxValue, int defaultValue, CancellationToken cancellationToken);

        /// <summary>
        /// Prompts the user to select a prefix position for a file.
        /// </summary>
        /// <param name="filename">The name of the file.</param>
        /// <param name="maxValue">The maximum position value allowed.</param>
        /// <param name="defaultValue">The default position value.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A tuple containing whether the operation was aborted and the selected position value.</returns>
        (bool IsAborted, int Value) PromptSelectPrefixPosition(string filename, int maxValue, int defaultValue, CancellationToken cancellationToken);


        /// <summary>
        /// Prompts the user to select a revision length from a file at a specified position.
        /// </summary>
        /// <param name="filename">The name or path of the file.</param>
        /// <param name="position">The position within the file.</param>
        /// <param name="maxValue">The maximum allowed value for the revision length.</param>
        /// <param name="defaultValue">The default value to use if no selection is made.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing whether the operation was aborted and the selected value.</returns>
        (bool IsAborted, int Value) PromptSelectRevisionLength(string filename, int position, int maxValue, int defaultValue, CancellationToken cancellationToken);

        /// <summary>
        /// Prompts the user to select a Revision position for a file.
        /// </summary>
        /// <param name="filename">The name of the file.</param>
        /// <param name="maxValue">The maximum position value allowed.</param>
        /// <param name="defaultValue">The default position value.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A tuple containing whether the operation was aborted and the selected position value.</returns>
        (bool IsAborted, int Value) PromptSelectRevisionPosition(string filename, int maxValue, int defaultValue, CancellationToken cancellationToken);


        /// <summary>
        /// Prompts the user to select a version length from a file at a specified position.
        /// </summary>
        /// <param name="filename">The name or path of the file.</param>
        /// <param name="position">The position within the file.</param>
        /// <param name="maxValue">The maximum allowed value for the version length.</param>
        /// <param name="defaultValue">The default value to use if no selection is made.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing whether the operation was aborted and the selected value.</returns>
        (bool IsAborted, int Value) PromptSelectVersionLength(string filename, int position, int maxValue, int defaultValue, CancellationToken cancellationToken);

        /// <summary>
        /// Prompts the user to select a Version position for a file.
        /// </summary>
        /// <param name="filename">The name of the file.</param>
        /// <param name="maxValue">The maximum position value allowed.</param>
        /// <param name="defaultValue">The default position value.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A tuple containing whether the operation was aborted and the selected position value.</returns>
        (bool IsAborted, int Value) PromptSelectVersionPosition(string filename, int maxValue, int defaultValue, CancellationToken cancellationToken);

        /// <summary>
        /// Prompts the user to select a prefix length from a file at a specified position.
        /// </summary>
        /// <param name="filename">The name or path of the file.</param>
        /// <param name="position">The position within the file.</param>
        /// <param name="maxValue">The maximum allowed value for the prefix length.</param>
        /// <param name="defaultValue">The default value to use if no selection is made.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing whether the operation was aborted, the selected value, and the prefix value string.</returns>
        (bool IsAborted, int Value, string PrefixValue) PromptSelectPrefixLength(string filename, int position, int maxValue, int defaultValue, CancellationToken cancellationToken);


        /// <summary>
        /// Prompts the user to select a number position for a file.
        /// </summary>
        /// <param name="filename">The name of the file.</param>
        /// <param name="maxValue">The maximum position value allowed.</param>
        /// <param name="defaultValue">The default position value.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A tuple containing whether the operation was aborted and the selected position value.</returns>
        (bool IsAborted, int Value) PromptSelectNumberPosition(string filename, int maxValue, int defaultValue, CancellationToken cancellationToken);


        /// <summary>
        /// Prompts the user to select a number length for a file at a specified position.
        /// </summary>
        /// <param name="filename">The name of the file.</param>
        /// <param name="position">The position within the file.</param>
        /// <param name="maxValue">The maximum allowed value for the number length.</param>
        /// <param name="defaultValue">The default value to use if no selection is made.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing whether the operation was aborted and the selected number length value.</returns>
        (bool IsAborted, int Value) PromptSelectNumberLength(string filename, int position, int maxValue, int defaultValue, CancellationToken cancellationToken);



        /// <summary>
        /// Prompts the user to provide existing fields from a filename.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the prompt operation.</param>
        /// <returns>A tuple containing a flag indicating whether the operation was aborted and an array of field values from the filename.</returns>
        (bool IsAborted, string[] FieldsFromFileAdr) PromptFieldsFromFileAdr(CancellationToken cancellationToken);

        /// <summary>
        /// Prompts the user for sample file migration.
        /// </summary>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a flag indicating whether the operation was aborted and the sample file migration result.</returns>
        (bool IsAborted, string SampleFileMigration) PromptSampleFileMigration(CancellationToken cancellationToken);

        /// <summary>
        /// Prompts the user to select fields using an interactive explorer and returns the result along with an
        /// indicator of whether the operation was aborted.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation before completion.</param>
        /// <returns>A tuple containing a boolean value that indicates whether the operation was aborted, and an array of strings
        /// representing the selected fields. If the operation is aborted, the array may be empty.</returns>
        (bool IsAborted, string[] FieldsExplorer) PromptFieldsExplorer(CancellationToken cancellationToken);

        /// <summary>
        /// Prompts the user to choose whether to display an existing report or create a new one.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the prompt operation.</param>
        /// <returns>A tuple indicating whether the operation was aborted and whether the user chose to create a new report. The
        /// first value is <see langword="true"/> if the operation was aborted; otherwise, <see langword="false"/>. The
        /// second value is <see langword="true"/> if the user chose to create a new report; otherwise, <see
        /// langword="false"/>.</returns>
        (bool IsAborted, bool IsCreatingReport) PromptOptionShowOrCreateReport(CancellationToken cancellationToken);

        /// <summary>
        /// Prompts the user to select an input file for generating a report and returns the result along with an indicator of whether the operation was aborted. 
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the prompt operation.</param>
        /// <returns>A tuple containing a boolean indicating whether the operation was aborted and a string representing the selected file name.</returns>
        (bool IsAborted, string Filename) PromptInputFileReport(CancellationToken cancellationToken);

        /// <summary>
        /// Prompts the user to select a file from a list of found files and returns the result of the selection. 
        /// </summary>
        /// <param name="foundfiles">An array of ADR file name components representing the available files to choose from.</param>
        /// <param name="fields">An array of field names to display in the table.</param>
        /// <param name="folderrepoadr">The folder path of the ADR repository.</param>
        /// <param name="adrPlusRepoConfig">The repository configuration used to resolve ADR file details.</param>
        /// <returns>A tuple containing a boolean indicating whether the operation was aborted and a string representing the selected file name.</returns>
        (bool IsAborted, string FileSelectd) PromptTableExplorer(AdrFileNameComponents[] foundfiles, string[] fields, string folderrepoadr, AdrPlusRepoConfig adrPlusRepoConfig);

        /// <summary>
        /// Prompts the user to select ADR migrations to display and returns the result of the selection.
        /// </summary>
        /// <param name="adrs">An array of ADR file name components representing the available ADR migrations to choose from.</param>
        /// <param name="adrPlusRepo">The repository configuration used to resolve ADR migration details.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A tuple containing a boolean indicating whether the operation was aborted and an integer representing the
        /// number of selected ADR migrations.</returns>
        (bool IsAborted, int CountSelected) PromptShowAdrsMigrations(AdrFileNameComponents[] adrs, AdrPlusRepoConfig adrPlusRepo, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current position of the cursor in the console.  
        /// </summary>
        /// <returns>A tuple containing the left and top positions of the cursor.</returns>
        (int left, int top) PromptCursorPosition();

        /// <summary>
        /// Moves the cursor to a new position in the console.
        /// </summary>
        /// <param name="left">The left position to move the cursor to.</param>
        /// <param name="top">The top position to move the cursor to.</param>
        void PromptMovePosition(int left,int top);

        /// <summary>
        /// Checks if the current operation was aborted by the user pressing Ctrl+C.
        /// </summary>
        /// <returns> <c>true</c> if the operation was aborted by Ctrl+C; otherwise, <c>false</c>.</returns>
        bool PromptIsAbortedByCtrlC();

        /// <summary>
        /// Ensures that the console culture settings are properly configured based on the provided application configuration. 
        /// </summary>
        /// <param name="config">
        /// The application configuration containing culture settings to apply to the console. This may include settings such as language, date formats, and other culture-specific configurations that affect how information is displayed in the console.
        /// </param>
        void PromptEnsureCulture(AdrPlusConfig config);

        /// <summary>
        /// Gets the current position of the cursor.
        /// </summary>
        /// <returns>A tuple containing the left and top positions of the cursor.</returns>
        (int left, int top) PromptGetCursorPosition();

        void PromptWriteWait(string message);

        /// <summary>
        /// Clears the wait message from the console at current positions and set positions of the cursor. 
        /// </summary>
        /// <param name="position">
        /// A tuple containing the left and top positions where the wait message was displayed, used to clear the message and reset the cursor position.
        /// </param>
        void PromptClearWaitText((int left, int top) position);

        /// <summary>
        /// Writes an informational message to the console.
        /// </summary>
        void PromptWriteInfo(string message);

        /// <summary>
        /// Writes a message to the console indicating that an operation is being resumed before a wait or pause.
        /// </summary>
        void PromptWriteSummary(string message);

        /// <summary>
        /// Writes a success message to the console.
        /// </summary>
        void PromptWriteSuccess(string message);

        /// <summary>
        /// Writes an error message to the console.
        /// </summary>
        void PromptWriteError(string message);

        /// <summary>
        /// Writes help information to the console.
        /// </summary>
        void PromptWriteHelp(string helpText);

        /// <summary>
        /// Writes the specified command to the output stream.
        /// </summary>
        /// <param name="command">The command string to write.</param>
        void PromptWriteStartCommand(string command);

        /// <summary>
        /// Writes a message indicating that the specified command has finished executing. 
        /// </summary>
        /// <param name="command">The command string to write.</param>
        void PromptWriteFinishedCommand(string command);

        /// <summary>
        /// Displays a welcome message including the specified application version.
        /// </summary>
        /// <param name="appVersion">The version of the application to include in the welcome message.</param>
        void PromptShowWellcome(string appVersion);

        /// <summary>
        /// Configures the prompt settings. 
        /// </summary>
        /// <param name="config">The configuration settings to apply to the prompt.</param>
        void PromptConfigure(AdrPlusConfig config);

        /// <summary>
        /// Displays a banner with the specified text. 
        /// </summary>
        /// <param name="bannerText">The text to display in the banner.</param>
        void PromptShowBanner(string bannerText);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message">The message to display to the user.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A boolean indicating whether the user pressed abort key.</returns>
        bool PromptPressAnyKeyToContinue(string message,CancellationToken cancellationToken);

        /// <summary>
        /// Enables or disables the ability for the user to abort an operation by pressing the Escape key during prompts. 
        /// </summary>
        /// <param name="enabled"> A boolean value indicating whether pressing the Escape key should abort the current operation. If set to true, the user can press Escape to cancel prompts; if false, the Escape key will not have any effect on prompt cancellation.
        /// </param>
        void PromptEnabledEscToAbort(bool enabled);

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
        /// Prompts the user to edit the behavior when no arguments are provided and returns the result along with an abort status.
        /// </summary>
        /// <param name="fieldsJson">The fields metadata used to guide the prompt.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the entered behavior content.</returns>
        (bool IsAborted, string Content) PromptEditFieldBehaviorWithoutArgs(FieldsJson fieldsJson, CancellationToken cancellationToken = default);

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
        /// Prompts the user to confirm whether to use an empty template and returns the result along with an abort status.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the selected boolean value.</returns>
        (bool IsAborted, bool Content) PromptEmptyTemplate(CancellationToken cancellationToken = default);

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
        /// Prompts the user to edit the header  text and returns the result along with an abort status.
        /// </summary>
        /// <param name="fieldsJson">The fields metadata used to guide the prompt.</param>
        /// <param name="maxlength">Max length of text</param>
        /// <param name="sugestion">Sugestion to text</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the entered disclaimer text.</returns>
        (bool IsAborted, string Content) PromptEditFieldHeaderText(FieldsJson fieldsJson, int maxlength, string sugestion, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to select a logical drive from available drives.
        /// </summary>
        /// <param name="message">The message to display to the user.</param>
        /// <param name="fileSystemService">The file system service to enumerate available drives.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the selected drive path.</returns>
        (bool IsAborted, string Content) PromptSelectLogicalDrive(string message, IFileSystemService fileSystemService, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to select the repository folder.
        /// </summary>
        /// <param name="message">The message to display to the user.</param>
        /// <param name="checknitCmd">Whether to check for init command requirements.</param>
        /// <param name="root">The root directory path to start browsing from.</param>
        /// <param name="fileSystemService">The file system service to use for directory operations.</param>
        /// <param name="validateJsonConfig">The service to validate JSON configuration.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the selected folder path.</returns>
        (bool IsAborted, string Content) PromptSelectFolderPath(string message, bool checknitCmd, string root, IFileSystemService fileSystemService, IValidateJsonConfig validateJsonConfig, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to select a folder.
        /// </summary>
        /// <param name="root">The root directory path to start browsing from.</param>
        /// <param name="fileSystemService">The file system service to use for directory operations.</param>
        /// <param name="validateJsonConfig">The service to validate JSON configuration.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the selected folder path.</returns>
        (bool IsAborted, string Content) PromptSelectFolderRepositoryAdr(string root, IFileSystemService fileSystemService, IValidateJsonConfig validateJsonConfig, CancellationToken cancellationToken = default);

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
        /// <param name="adrPlusRepo">The repository configuration.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing abort status, array of domains, and any exception that occurred.</returns>
        (bool IsAborted, string[] domains, Exception? Content) PromptGetArrayDomainsAdr(IFileSystemService fileSystemService, string path, AdrPlusRepoConfig adrPlusRepo, CancellationToken cancellationToken = default);

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
        (bool IsAborted, DateTime Content) PromptCalendar(string message, DateTime dateref, AdrPlusConfig adrPlusRepo, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts the user to select an ADR from a list of latest ADR files.
        /// </summary>
        /// <param name="files">The array of ADR files to choose from.</param>
        /// <param name="adrPlusRepoConfig">The repository configuration.</param>
        /// <param name="validselect">Validation function that checks whether a selected ADR is valid and returns a message when invalid.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing a boolean indicating if the operation was aborted and the selected ADR information.</returns>
        (bool IsAborted, AdrFileNameComponents? info) PromptSelecAdrs(AdrFileNameComponents[] files, AdrPlusRepoConfig adrPlusRepoConfig, Func<AdrFileNameComponents, (bool, string?)> validselect, CancellationToken cancellationToken = default);

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
