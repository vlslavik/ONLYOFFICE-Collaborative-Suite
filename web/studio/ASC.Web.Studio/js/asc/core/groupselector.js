/*
(c) Copyright Ascensio System SIA 2010-2014

This program is a free software product.
You can redistribute it and/or modify it under the terms 
of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of 
any third-party rights.

This program is distributed WITHOUT ANY WARRANTY; without even the implied warranty 
of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see 
the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html

You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.

The  interactive user interfaces in modified source and object code versions of the Program must 
display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
 
Pursuant to Section 7(b) of the License you must retain the original Product logo when 
distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under 
trademark law for use of our trademarks.
 
All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode
*/

/*
    Copyright (c) Ascensio System SIA 2013. All rights reserved.
    http://www.teamlab.com
*/
if (typeof (ASC) == "undefined")
    ASC = { };
if (typeof (ASC.Controls) == "undefined")
    ASC.Controls = { };

ASC.Controls.GroupSelector = function(selectorID, mobileVersion, withGroupEveryone, withGroupAdmin) {
    this.ID = selectorID;

    this.SelectedGroup = null;
    this.Groups = [];
    this._groups = [];

    this.DisabledGroupIds = new Array();
    this.MobileVersion = mobileVersion === true;

    var _groupSelector = this;

    this.AdditionalFunction = null;

    this.Open = function() {

        this.ClearFilter();

        if (this.MobileVersion)
            return;

        var pos = jq("#groupSelectorBtn_" + this.ID).position();

        jq("#groupSelectorContainer_" + this.ID).css({
                display: "block"
            });

        jq("#grpselector_filter_" + this.ID).focus();
    };

    this.Close = function() {
        if (!this.MobileVersion)
            jq("#groupSelectorContainer_" + this.ID).hide();

        if (this.SelectedGroup != null && this.AdditionalFunction != null)
            this.AdditionalFunction(this.SelectedGroup);
    };

    this.SuggestGroups = function() {
        if (this.MobileVersion)
            return;

        var text = jq("#grpselector_filter_" + this.ID).val().trim().toLowerCase();
        if (text == "") {
            this.ClearFilter();
        }

        var filtered = [];
        for (var i = 0; i < this._groups.length; i++) {
            if (this._groups[i].Name.toLowerCase().indexOf(text) >= 0)
                filtered.push(this._groups[i]);
        }

        jq("#grpselector_groupList_" + this.ID).html(jq.tmpl("groupSelectorListTemplate", { Groups: filtered }));
    };

    this.HideGroup = function(groupID, hide) {
        if (hide == undefined)
            hide = true;

        if (hide) {
            this.DisabledGroupIds.push(groupID);
        } else {
            var tmpDisabledGroup = [];
            for (var i = 0; i < this.DisabledGroupIds.length; i++) {
                if (this.DisabledGroupIds[i] != groupID) {
                    tmpDisabledGroup.push(this.DisabledGroupIds[i]);
                }
            }
            this.DisabledGroupIds = tmpDisabledGroup;
        }

        this.ClearFilter();
    };

    this.DisplayAll = function() {
        this.DisabledGroupIds = new Array();
        this.ClearFilter();
    };

    this.ClearFilter = function() {
        this._groups = [];
        for (var i = 0, n = _groupSelector.Groups.length; i < n; i++) {
            var disabled = false;
            for (var k = 0, m = _groupSelector.DisabledGroupIds.length; k < m; k++) {
                if (this.DisabledGroupIds[k] == _groupSelector.Groups[i].Id) {
                    disabled = true;
                    break;
                }
            }
            if (!disabled) {
                this._groups.push(_groupSelector.Groups[i]);
            }
        }

        if (this.MobileVersion) {
            jq("#grpselector_mgroupList_" + this.ID).html(jq.tmpl("groupSelectorListTemplate", { Groups: this._groups }));
            jq("#grpselector_mgroupList_" + this.ID + " option[value='" + this.SelectedGroup + "']").attr("selected", "selected");
            return;
        }

        jq("#grpselector_filter_" + this.ID).val("");
        jq("#grpselector_groupList_" + this.ID).html(jq.tmpl("groupSelectorListTemplate", { Groups: this._groups }));
    };

    if (typeof (ASC.Resources.Master.ApiResponses_Groups) === "undefined") return;

    if (this.MobileVersion === true) {
        this.Groups.push(ASC.Resources.Master.GroupSelector_MobileVersionGroup);
    }
    if (withGroupEveryone) {
        this.Groups.push(ASC.Resources.Master.GroupSelector_WithGroupEveryone);
    }
    if (withGroupAdmin) {
        this.Groups.push(ASC.Resources.Master.GroupSelector_WithGroupAdmin);
    }

    for (var i = 0, n = ASC.Resources.Master.ApiResponses_Groups.response.length; i < n; i++) {
        this.Groups.push({
            Id: ASC.Resources.Master.ApiResponses_Groups.response[i].id,
            Name: ASC.Resources.Master.ApiResponses_Groups.response[i].name
        });
    }

    jq("#grpselector_filter_" + this.ID).on("keyup", function() { _groupSelector.SuggestGroups(); });
    jq("#grpselector_clearFilterBtn_" + this.ID).on("click", function() { _groupSelector.ClearFilter(); });

    jq(document).click(function(event) {
        if (!jq((event.target) ? event.target : event.srcElement).parents().addBack().is("#groupSelectorBtn_" + selectorID + ", #groupSelectorContainer_" + selectorID))
            jq("#groupSelectorContainer_" + selectorID).hide();
    });

    if (mobileVersion) {
        jq("#grpselector_mgroupList_" + selectorID).bind("change", function() {
            var grpId = jq(this).val();
            if (grpId == "" || grpId == "-1") return;
            jq(_groupSelector.Groups).each(function(i, gpr) {
                if (gpr.Id == grpId) {
                    _groupSelector.SelectedGroup = gpr;
                    return false;
                }
            });

            _groupSelector.Close();
        });
    } else {
        jq("#groupSelectorBtn_" + selectorID).bind("click", function(evnt) {
            _groupSelector.Open();
        });

        jq("#grpselector_clearFilterBtn_" + selectorID).bind("click", function() {
            _groupSelector.ClearFilter();
        });

        jq("#grpselector_groupList_" + selectorID).bind("click", function(evnt) {
            if (!jq(evnt.target).is("div.group"))
                return;

            var grpId = jq(evnt.target).attr("data");
            if (grpId == "") return;
            jq(_groupSelector.Groups).each(function(i, gpr) {
                if (gpr.Id == grpId) {
                    _groupSelector.SelectedGroup = gpr;
                    return false;
                }
            });

            _groupSelector.Close();
        });
    }

};