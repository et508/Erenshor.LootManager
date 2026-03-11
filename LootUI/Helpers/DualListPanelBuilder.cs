using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LootManager
{
    internal struct DualListRefs
    {
        public RectTransform LeftContent;
        public ScrollRect    LeftScroll;
        public GameObject    RowTemplate;

        public RectTransform RightContent;
        public ScrollRect    RightScroll;

        public TMP_InputField FilterInput;
    }

    internal static class DualListPanelBuilder
    {
        private const float RowH    = 24f;
        private const float FilterH = 26f;

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

            if (extraBuilder != null)
                extraBuilder(body.transform);

            refs.FilterInput = LootUIController.MakeInputField("filterInput", body.transform, filterPlaceholder);
            refs.FilterInput.gameObject.AddComponent<LayoutElement>().preferredHeight = FilterH;

            // Column headers
            var headerRow = new GameObject("ColumnHeaders");
            headerRow.AddComponent<RectTransform>().SetParent(body.transform, false);
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

            // Dual scroll lists
            var listsRow = new GameObject("ListsRow");
            listsRow.AddComponent<RectTransform>().SetParent(body.transform, false);
            var lrHL = listsRow.AddComponent<HorizontalLayoutGroup>();
            lrHL.spacing                = 8;
            lrHL.childForceExpandWidth  = false;
            lrHL.childForceExpandHeight = true;
            lrHL.childControlWidth      = true;
            lrHL.childControlHeight     = true;
            listsRow.AddComponent<LayoutElement>().flexibleHeight = 1;

            RectTransform leftVP, leftContent;
            refs.LeftScroll  = LootUIController.MakeScrollView("leftScroll", listsRow.transform, out leftVP, out leftContent);
            refs.LeftContent = leftContent;
            refs.LeftScroll.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            RectTransform rightVP, rightContent;
            refs.RightScroll  = LootUIController.MakeScrollView("rightScroll", listsRow.transform, out rightVP, out rightContent);
            refs.RightContent = rightContent;
            refs.RightScroll.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            refs.RowTemplate = LootUIController.MakeRowTemplate("rowTemplate", leftContent, RowH);

            // Hint row
            var hint = LootUIController.MakeTMP("hintLabel", body.transform);
            hint.text      = "Double-click an item to move it";
            hint.color     = LootUIController.C_TextMuted;
            hint.fontSize  = 9;
            hint.fontStyle = FontStyles.Italic;
            hint.alignment = TextAlignmentOptions.Center;
            hint.gameObject.AddComponent<LayoutElement>().preferredHeight = 18;

            return refs;
        }
    }
}