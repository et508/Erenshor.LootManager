// DualListPanelBuilder.cs
// Shared helper that builds the standard two-column item list layout used by
// Blacklist, Whitelist, and Banklist panels. Returns direct widget references.

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LootManager
{
    /// <summary>
    /// Holds all the widget references produced by DualListPanelBuilder.Build().
    /// </summary>
    internal struct DualListRefs
    {
        // Left scroll list (all items / available)
        public RectTransform LeftContent;
        public ScrollRect    LeftScroll;
        public GameObject    RowTemplate;   // shared template (SetActive false)

        // Right scroll list (items in the list)
        public RectTransform RightContent;
        public ScrollRect    RightScroll;

        // Filter input at the top
        public TMP_InputField FilterInput;

        // Add / Remove buttons
        public Button AddBtn;
        public Button RemoveBtn;
    }

    internal static class DualListPanelBuilder
    {
        private const float RowH      = 24f;
        private const float BtnW      = 70f;
        private const float FilterH   = 26f;
        private const float BtnAreaH  = 32f;

        /// <summary>
        /// Builds a two-column virtual-list panel inside <paramref name="panelRoot"/>.
        /// Optionally appends extra controls <paramref name="extraBuilder"/> above the lists.
        /// </summary>
        public static DualListRefs Build(
            GameObject panelRoot,
            string leftTitle, string rightTitle,
            string filterPlaceholder = "Filter...",
            System.Action<Transform> extraBuilder = null)
        {
            var refs = new DualListRefs();
            if (panelRoot == null) return refs;

            var body = new GameObject("dualListBody");
            var bodyRT = body.AddComponent<RectTransform>();
            bodyRT.SetParent(panelRoot.transform, false);
            LootUIController.StretchFull(bodyRT);

            var vl = body.AddComponent<VerticalLayoutGroup>();
            vl.padding                = new RectOffset(8, 8, 8, 8);
            vl.spacing                = 6;
            vl.childForceExpandWidth  = true;
            vl.childForceExpandHeight = false;
            vl.childControlWidth      = true;
            vl.childControlHeight     = true;

            // Optional extra controls (toggles, dropdowns) at top
            if (extraBuilder != null)
                extraBuilder(body.transform);

            // Filter row
            refs.FilterInput = LootUIController.MakeInputField("filterInput", body.transform, filterPlaceholder);
            refs.FilterInput.gameObject.AddComponent<LayoutElement>().preferredHeight = FilterH;

            // Column headers row
            var headerRow = new GameObject("ColumnHeaders");
            var hrRT = headerRow.AddComponent<RectTransform>();
            hrRT.SetParent(body.transform, false);
            var hrHL = headerRow.AddComponent<HorizontalLayoutGroup>();
            hrHL.spacing                = 8;
            hrHL.childForceExpandWidth  = false;
            hrHL.childForceExpandHeight = false;
            hrHL.childControlWidth      = true;
            hrHL.childControlHeight     = true;
            headerRow.AddComponent<LayoutElement>().preferredHeight = 18;

            var leftHdr = LootUIController.MakeTMP("leftHdr", headerRow.transform);
            leftHdr.text      = leftTitle;
            leftHdr.color     = LootUIController.C_TextMuted;
            leftHdr.fontSize  = 10;
            leftHdr.fontStyle = FontStyles.Bold;
            leftHdr.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            var rightHdr = LootUIController.MakeTMP("rightHdr", headerRow.transform);
            rightHdr.text      = rightTitle;
            rightHdr.color     = LootUIController.C_TextMuted;
            rightHdr.fontSize  = 10;
            rightHdr.fontStyle = FontStyles.Bold;
            rightHdr.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            // List columns row (fills available height)
            var listsRow = new GameObject("ListsRow");
            var lrRT = listsRow.AddComponent<RectTransform>();
            lrRT.SetParent(body.transform, false);
            var lrHL = listsRow.AddComponent<HorizontalLayoutGroup>();
            lrHL.spacing                = 8;
            lrHL.childForceExpandWidth  = false;
            lrHL.childForceExpandHeight = true;
            lrHL.childControlWidth      = true;
            lrHL.childControlHeight     = true;
            var lrLE = listsRow.AddComponent<LayoutElement>();
            lrLE.flexibleHeight = 1;

            // Left scroll
            RectTransform leftVP, leftContent;
            refs.LeftScroll = LootUIController.MakeScrollView("leftScroll", listsRow.transform, out leftVP, out leftContent);
            refs.LeftContent = leftContent;
            refs.LeftScroll.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            // Right scroll
            RectTransform rightVP, rightContent;
            refs.RightScroll = LootUIController.MakeScrollView("rightScroll", listsRow.transform, out rightVP, out rightContent);
            refs.RightContent = rightContent;
            refs.RightScroll.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            // Shared row template (parented under left content, kept inactive)
            refs.RowTemplate = LootUIController.MakeRowTemplate("rowTemplate", leftContent, RowH);

            // Button row
            var btnRow = new GameObject("BtnRow");
            var brRT = btnRow.AddComponent<RectTransform>();
            brRT.SetParent(body.transform, false);
            var brHL = btnRow.AddComponent<HorizontalLayoutGroup>();
            brHL.spacing                = 8;
            brHL.childForceExpandWidth  = false;
            brHL.childForceExpandHeight = false;
            brHL.childControlWidth      = false;
            brHL.childControlHeight     = true;
            btnRow.AddComponent<LayoutElement>().preferredHeight = BtnAreaH;

            // Spacer
            var spacer = new GameObject("Spacer");
            spacer.AddComponent<RectTransform>().SetParent(brRT, false);
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1;

            refs.AddBtn = LootUIController.MakeButton("addBtn", brRT, "Add ›",
                LootUIController.C_TextPri, LootUIController.C_BtnNormal);
            refs.AddBtn.gameObject.AddComponent<LayoutElement>().preferredWidth = BtnW;

            refs.RemoveBtn = LootUIController.MakeButton("removeBtn", brRT, "‹ Remove",
                LootUIController.C_TextPri, LootUIController.C_BtnNormal);
            refs.RemoveBtn.gameObject.AddComponent<LayoutElement>().preferredWidth = BtnW;

            return refs;
        }
    }
}