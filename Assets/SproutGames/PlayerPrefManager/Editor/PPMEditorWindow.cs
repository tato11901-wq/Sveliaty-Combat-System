using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;

namespace SproutGames.PlayerPrefManager
{

    public class PPMEditorWindow : EditorWindow
    {

        PPMPrefDataHandler prefDataHandler;
        StyleSheet styleSheet;

        ListView m_LeftPane;
        ScrollView m_RightPane;

        TextField m_PrefNameText;
        DropdownField m_PrefDataType;
        TextField m_PrefValue;
        Button m_EditButton, m_SaveButton, m_CancelButton, m_DeleteButton;

        TextField m_NewPrefKey, m_NewPrefValue;
        DropdownField m_NewPrefType;

        List<PrefData> m_filteredPrefData;

        ToolbarSearchField m_searchField;
        DropdownField m_typeFilterDropdown;
        string m_FilterSearchKey = "";
        string m_FilterTypeKey = "All";

        [SerializeField] int m_SelectedIndex = 0;

        [MenuItem("Tools/SproutStudio/PlayerPrefs Manager")]
        private static void ShowWindow()
        {
            PPMEditorWindow wnd = GetWindow<PPMEditorWindow>();
            wnd.titleContent = new GUIContent("PlayerPrefs Manager");

            // Limit size of the window.
            wnd.minSize = new Vector2(720, 450);
            wnd.maxSize = new Vector2(1920, 720);
        }

        private void CreateGUI()
        {
            styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets\\SproutGames\\PlayerPrefManager\\Stylesheets\\PPMEditorStyles.uss");

            prefDataHandler = new PPMPrefDataHandler();
            m_filteredPrefData = new List<PrefData>();

            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;
            root.styleSheets.Add(styleSheet);

            SearchBarDesign();
            DataContainerDesign();
            SavePlayerPrefFooterDesign();
            ResetEditControls();
            FillPrefData();
        }

        #region Design
        private void SearchBarDesign()
        {
            var headerBar = new Toolbar();
            headerBar.AddToClassList("headerBar");
            rootVisualElement.Add(headerBar);

            m_searchField = new ToolbarSearchField();
            headerBar.Add(m_searchField);
            m_searchField.RegisterValueChangedCallback(SearchFilterPrefs);

            m_typeFilterDropdown = new DropdownField(new List<String> { "All", "int", "float", "string" }, 0, null, null);
            headerBar.Add(m_typeFilterDropdown);
            m_typeFilterDropdown.RegisterValueChangedCallback(TypeDropDownFilterPrefs);

            var deleteAllPrefsButton = new Button(() => ClearAllPrefs());
            deleteAllPrefsButton.AddToClassList("deleteAllPrefsButton");
            deleteAllPrefsButton.text = "Delete All Prefs";
            headerBar.Add(deleteAllPrefsButton);
        }

        private void DataContainerDesign()
        {

            // Create a two-pane view with the left pane being fixed.
            var splitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);
            rootVisualElement.Add(splitView);

            m_LeftPane = new ListView(); ;
            splitView.Add(m_LeftPane);
            m_RightPane = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            splitView.Add(m_RightPane);

            //Arrangement of Right Pane
            //Data Container
            var prefDataContainer = new Box();
            prefDataContainer.AddToClassList("prefDataContainer");
            m_RightPane.Add(prefDataContainer);

            m_PrefNameText = new TextField("PlayerPref Name");
            prefDataContainer.Add(m_PrefNameText);

            m_PrefDataType = new DropdownField("Pref Data Type", new List<String> { "int", "float", "string" }, 0);
            prefDataContainer.Add(m_PrefDataType);

            m_PrefValue = new TextField("PlayerPref value");
            prefDataContainer.Add(m_PrefValue);

            //Controls Container
            var prefControlContainer = new Box();
            prefControlContainer.AddToClassList("prefControlContainer");
            m_RightPane.Add(prefControlContainer);

            m_EditButton = new Button(() => EditPrefData());
            m_EditButton.text = "Edit";
            prefControlContainer.Add(m_EditButton);

            m_SaveButton = new Button(() => SavePrefData());
            m_SaveButton.text = "Save";
            prefControlContainer.Add(m_SaveButton);

            m_CancelButton = new Button(() => CancelEditPrefData());
            m_CancelButton.text = "Cancel";
            prefControlContainer.Add(m_CancelButton);

            m_DeleteButton = new Button(() => DeletePref());
            m_DeleteButton.text = "Delete";
            prefControlContainer.Add(m_DeleteButton);

        }

        private void SavePlayerPrefFooterDesign()
        {
            var footerBar = new Toolbar();
            footerBar.AddToClassList("footerBar");
            rootVisualElement.Add(footerBar);

            //AddNewPlayerPref Heading
            var footerBarHeadingBox = new Box();
            footerBar.Add(footerBarHeadingBox);
            footerBarHeadingBox.AddToClassList("footerBarHeadingBox");
            footerBarHeadingBox.style.unityTextAlign = TextAnchor.MiddleLeft;

            var newPlayerPrefTitle = new Label("Add New Playerpref");
            footerBarHeadingBox.Add(newPlayerPrefTitle);
            newPlayerPrefTitle.StretchToParentSize();

            //AddNewPlayerPref Data
            var newPrefNameBox = new Box();
            newPrefNameBox.AddToClassList("background-transparent");
            footerBar.Add(newPrefNameBox);
            var newPrefkeyLabel = new Label("PlayerPref Key");
            newPrefNameBox.Add(newPrefkeyLabel);
            m_NewPrefKey = new TextField();
            newPrefNameBox.Add(m_NewPrefKey);

            var newPrefTypeBox = new Box();
            newPrefTypeBox.AddToClassList("background-transparent");
            footerBar.Add(newPrefTypeBox);
            var newPrefTypeLabel = new Label("PlayerPref Type");
            newPrefTypeBox.Add(newPrefTypeLabel);
            m_NewPrefType = new DropdownField("", new List<String> { "int", "float", "string" }, 0);
            newPrefTypeBox.Add(m_NewPrefType);

            var newPrefValueBox = new Box();
            newPrefValueBox.AddToClassList("background-transparent");
            footerBar.Add(newPrefValueBox);
            var newPrefValueLabel = new Label("PlayerPref Value");
            newPrefValueBox.Add(newPrefValueLabel);
            m_NewPrefValue = new TextField();
            newPrefValueBox.Add(m_NewPrefValue);

            var newPrefControlBox = new Box();
            newPrefControlBox.AddToClassList("background-transparent");
            footerBar.Add(newPrefControlBox);
            var newPrefSaveLabel = new Label("Add PlayerPref");
            newPrefControlBox.Add(newPrefSaveLabel);
            var newPrefSaveButton = new Button(() => AddNewPlayerPref());
            newPrefControlBox.Add(newPrefSaveButton);
            newPrefSaveButton.text = "Save";

            var newPrefClearBox = new Box();
            newPrefClearBox.AddToClassList("background-transparent");
            footerBar.Add(newPrefClearBox);
            newPrefClearBox.style.paddingTop = 12.5f;
            var newPrefClearButton = new Button(() => ClearNewPrefData());
            newPrefClearBox.Add(newPrefClearButton);
            newPrefClearButton.text = "Clear";

        }
        #endregion Design

        #region Data
        private void FillPrefData()
        {
            m_filteredPrefData.AddRange(prefDataHandler.GetFilteredPrefData());

            // Initialize the list view with all Pref names
            m_LeftPane.makeItem = () => new Label();
            m_LeftPane.bindItem = (item, index) =>
            {
                (item as Label).text = m_filteredPrefData[index].keyname;
                (item as Label).style.unityTextAlign = TextAnchor.MiddleLeft;
                (item as Label).style.paddingLeft = 20;
            };
            m_LeftPane.itemsSource = m_filteredPrefData;
            m_LeftPane.selectedIndex = m_SelectedIndex;
            // React to the user's selection
            m_LeftPane.selectionChanged += (items) => { m_SelectedIndex = m_LeftPane.selectedIndex; };
            m_LeftPane.selectionChanged += FillPrefValues;
        }

        private void FillPrefValues(IEnumerable<object> enumerable)
        {
            m_PrefNameText.value = m_filteredPrefData[m_SelectedIndex].keyname;

            Type type = m_filteredPrefData[m_SelectedIndex].keyType;
            if (type == typeof(int))
            {
                m_PrefDataType.index = 0;
            }
            if (type == typeof(float))
            {
                m_PrefDataType.index = 1;
            }
            if (type == typeof(string))
            {
                m_PrefDataType.index = 2;
            }

            m_PrefValue.value = m_filteredPrefData[m_SelectedIndex].value.ToString();
        }

        private void SavePrefData()
        {
            prefDataHandler.SavePrefData(m_SelectedIndex, m_PrefValue.value);
            m_filteredPrefData = prefDataHandler.GetFilteredPrefData();
            ResetEditControls();
        }

        private void CancelEditPrefData()
        {
            m_PrefValue.value = m_filteredPrefData[m_SelectedIndex].value.ToString();
            ResetEditControls();
        }

        private void ReFetchFilteredPrefList()
        {
            m_filteredPrefData.Clear();
            m_filteredPrefData.AddRange(prefDataHandler.GetFilteredPrefData());
            m_LeftPane.Rebuild();
        }

        #endregion Data

        #region UI Controls
        private void EditPrefData()
        {
            m_PrefValue.SetEnabled(true);

            m_EditButton.SetEnabled(false);
            m_SaveButton.SetEnabled(true);
            m_CancelButton.SetEnabled(true);
        }

        private void ResetEditControls()
        {
            m_EditButton.SetEnabled(true);
            m_SaveButton.SetEnabled(false);
            m_CancelButton.SetEnabled(false);

            m_PrefValue.SetEnabled(false);
            m_PrefNameText.SetEnabled(false);
            m_PrefDataType.SetEnabled(false);
        }

        #endregion UI Controls

        #region Filtering
        private void SearchFilterPrefs(ChangeEvent<string> evt)
        {
            m_FilterSearchKey = evt.newValue;
            prefDataHandler.ApplyFilterPref(m_FilterSearchKey, m_FilterTypeKey);
            ReFetchFilteredPrefList();
        }

        private void TypeDropDownFilterPrefs(ChangeEvent<string> evt)
        {
            m_FilterTypeKey = evt.newValue;
            prefDataHandler.ApplyFilterPref(m_FilterSearchKey, m_FilterTypeKey);
            ReFetchFilteredPrefList();
        }

        #endregion Filtering

        #region AddNewPref

        private void AddNewPlayerPref()
        {
            prefDataHandler.AddNewPlayerPref(m_NewPrefKey.value, m_NewPrefType.value, m_NewPrefValue.value);

            ReFetchFilteredPrefList();

            m_NewPrefKey.value = "";
            m_NewPrefType.index = 0;
            m_NewPrefValue.value = "";
        }

        private void ClearNewPrefData()
        {
            m_NewPrefKey.value = "";
            m_NewPrefType.index = 0;
            m_NewPrefValue.value = "";
        }

        #endregion AddNewPref

        #region PrefDeletion
        private void ClearAllPrefs()
        {
            prefDataHandler.ClearAllPlayerPrefs();
            m_searchField.value = "";
            m_typeFilterDropdown.value = "All";
            ReFetchFilteredPrefList();
        }

        private void DeletePref()
        {
            prefDataHandler.DeletePlayerPref(m_SelectedIndex);
            ReFetchFilteredPrefList();
            ResetEditControls();

            //reseting edit control's datas
            m_PrefValue.value = "";
            m_PrefNameText.value = "";
            m_PrefDataType.value = "int";
        }

        #endregion PrefDeletion


    }
}