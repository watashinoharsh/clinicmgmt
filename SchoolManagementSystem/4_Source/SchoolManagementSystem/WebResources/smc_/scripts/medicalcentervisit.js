/* Medical Center Visit - form logic (model-driven app).
 * Register on the Medical Center Visit main form:
 *   OnLoad  -> SMC.Visit.onLoad      OnSave -> SMC.Visit.onSave
 *   OnChange of Treatment Type / Final Outcome Type / vitals -> handlers below (already wired in the solution).
 */
var SMC = SMC || {};
SMC.Visit = (function () {
    "use strict";

    // ---- field names ----
    var F = {
        treatmentType: "smc_treatmenttype",
        firstAid: "smc_firstaidwoundcare",
        rest: "smc_restinclinic",
        other: "smc_othertreatment",
        outcomeType: "smc_finaloutcometype",
        notifyParent: "smc_notifyparent",
        notificationMessage: "smc_notificationmessage",
        transferTo: "smc_transferto",
        bp: "smc_bloodpressure",
        heartRate: "smc_heartrate",
        respRate: "smc_respiratoryrate",
        temperature: "smc_temperature",
        tempUnit: "smc_temperatureunit"
    };
    var MED_GRID = "grid_medicationtreatment";
    var MED_SECTION = "sec_medication";
    var TAB_TREATMENT = "tab_treatment";

    // ---- option values ----
    var TREATMENT = { FIRST_AID: 813460000, MEDICATION: 813460001, REST: 813460002, OTHER: 813460003 };
    var OUTCOME = { RETURN: 813460000, SEND_HOME: 813460001, HOSPITAL: 813460002 };
    var TEMP_UNIT = { F: 813460000, C: 813460001 };

    // ---- safe ranges (inclusive) ----
    var SAFE = {
        systolic: { min: 90, max: 120 },
        diastolic: { min: 60, max: 80 },
        heartRate: { min: 60, max: 100 },
        respiratoryRate: { min: 12, max: 20 },
        tempC: { min: 36, max: 37.5 },
        tempF: { min: 96.8, max: 99.5 }
    };

    var saving = false;

    function attr(fc, name) { return fc.getAttribute(name); }
    function ctl(fc, name) { return fc.getControl(name); }
    function val(fc, name) { var a = attr(fc, name); return a ? a.getValue() : null; }
    function show(fc, name, visible) { var c = ctl(fc, name); if (c) { c.setVisible(visible); } }

    // ---------------- Treatment type ----------------
    function hasTreatment(fc, option) {
        var v = val(fc, F.treatmentType);
        return Array.isArray(v) && v.indexOf(option) !== -1;
    }

    function applyTreatment(fc) {
        show(fc, F.firstAid, hasTreatment(fc, TREATMENT.FIRST_AID));
        show(fc, F.rest, hasTreatment(fc, TREATMENT.REST));
        show(fc, F.other, hasTreatment(fc, TREATMENT.OTHER));

        var medication = hasTreatment(fc, TREATMENT.MEDICATION);
        var grid = ctl(fc, MED_GRID);
        if (grid) { grid.setVisible(medication); }
        var tab = fc.ui.tabs.get(TAB_TREATMENT);
        var section = tab ? tab.sections.get(MED_SECTION) : null;
        if (section) { section.setVisible(medication); }
    }

    function onTreatmentTypeChange(ctx) { applyTreatment(ctx.getFormContext()); }

    // ---------------- Final outcome ----------------
    function applyOutcome(fc, clearHidden) {
        var t = val(fc, F.outcomeType);
        var notify = (t === OUTCOME.RETURN || t === OUTCOME.SEND_HOME);
        var hospital = (t === OUTCOME.HOSPITAL);

        show(fc, F.notifyParent, notify);
        show(fc, F.notificationMessage, notify);
        show(fc, F.transferTo, hospital);

        if (clearHidden) {
            if (!notify) {
                if (attr(fc, F.notificationMessage)) { attr(fc, F.notificationMessage).setValue(null); }
                if (attr(fc, F.notifyParent)) { attr(fc, F.notifyParent).setValue(false); }
            }
            if (!hospital && attr(fc, F.transferTo)) { attr(fc, F.transferTo).setValue(null); }
        }
    }

    function onOutcomeTypeChange(ctx) { applyOutcome(ctx.getFormContext(), true); }

    // ---------------- Vitals ----------------
    function parseBp(text) {
        if (text === null || text === undefined || String(text).trim() === "") { return null; }
        var m = /^\s*(\d+)\s*\/\s*(\d+)\s*$/.exec(String(text));
        if (!m) { return false; }
        var s = parseInt(m[1], 10), d = parseInt(m[2], 10);
        if (!(s > 0 && d > 0)) { return false; }
        return { systolic: s, diastolic: d };
    }

    function warn(fc, id, msg) {
        if (msg) { fc.ui.setFormNotification(msg, "WARNING", id); }
        else { fc.ui.clearFormNotification(id); }
    }

    function outside(v, range) { return v < range.min || v > range.max; }

    function checkVitals(fc) {
        // Blood pressure: format first, then range
        var bpCtl = ctl(fc, F.bp);
        var bp = parseBp(val(fc, F.bp));
        if (bp === false) {
            if (bpCtl) { bpCtl.setNotification("Blood pressure must be in the format <systolic>/<diastolic>, e.g. 120/80 (whole numbers above 0).", "smc_bp_format"); }
            warn(fc, "smc_w_bp", null);
        } else {
            if (bpCtl) { bpCtl.clearNotification("smc_bp_format"); }
            if (bp && (outside(bp.systolic, SAFE.systolic) || outside(bp.diastolic, SAFE.diastolic))) {
                warn(fc, "smc_w_bp", "Blood pressure " + bp.systolic + "/" + bp.diastolic + " is outside the safe range (" +
                    SAFE.systolic.min + "-" + SAFE.systolic.max + ")/(" + SAFE.diastolic.min + "-" + SAFE.diastolic.max + ").");
            } else { warn(fc, "smc_w_bp", null); }
        }

        var hr = val(fc, F.heartRate);
        warn(fc, "smc_w_hr", (hr !== null && outside(hr, SAFE.heartRate)) ?
            "Heart rate " + hr + " bpm is outside the safe range (" + SAFE.heartRate.min + "-" + SAFE.heartRate.max + ")." : null);

        var rr = val(fc, F.respRate);
        warn(fc, "smc_w_rr", (rr !== null && outside(rr, SAFE.respiratoryRate)) ?
            "Respiratory rate " + rr + " is outside the safe range (" + SAFE.respiratoryRate.min + "-" + SAFE.respiratoryRate.max + ")." : null);

        var temp = val(fc, F.temperature), unit = val(fc, F.tempUnit), tmsg = null;
        if (temp !== null && unit !== null) {
            var isC = (unit === TEMP_UNIT.C);
            var range = isC ? SAFE.tempC : SAFE.tempF;
            if (outside(temp, range)) {
                tmsg = "Temperature " + temp + (isC ? " °C" : " °F") + " is outside the safe range (" + range.min + "-" + range.max + ").";
            }
        }
        warn(fc, "smc_w_temp", tmsg);
    }

    function onVitalChange(ctx) { checkVitals(ctx.getFormContext()); }

    // ---------------- Load / Save ----------------
    function onLoad(ctx) {
        var fc = ctx.getFormContext();
        applyTreatment(fc);
        applyOutcome(fc, false);
        checkVitals(fc);
    }

    function onSave(ctx) {
        var fc = ctx.getFormContext();
        var args = ctx.getEventArgs();
        if (saving) { return; }

        // Block the save when blood pressure has a value in the wrong format.
        if (parseBp(val(fc, F.bp)) === false) {
            args.preventDefault();
            checkVitals(fc);
            return;
        }

        // Medication removed from Treatment Type while Medication Treatment rows exist -> block, user deletes rows first.
        var tt = attr(fc, F.treatmentType);
        var isNew = fc.ui.getFormType() === 1;
        if (tt && tt.getIsDirty() && !hasTreatment(fc, TREATMENT.MEDICATION) && !isNew) {
            args.preventDefault();
            var id = fc.data.entity.getId().replace(/[{}]/g, "");
            Xrm.WebApi.retrieveMultipleRecords("smc_medicationtreatment",
                "?$select=smc_medicationtreatmentid&$top=1&$filter=_smc_medicalcentervisit_value eq " + id).then(function (r) {
                    if (r.entities.length > 0) {
                        Xrm.Navigation.openAlertDialog({
                            title: "Medication Treatment exists",
                            text: "This visit still has Medication Treatment records. Delete them first, or keep 'Medication' selected in Treatment Type."
                        });
                    } else {
                        saving = true;
                        fc.data.save().then(function () { saving = false; }, function () { saving = false; });
                    }
                });
        }
    }

    return {
        onLoad: onLoad,
        onSave: onSave,
        onTreatmentTypeChange: onTreatmentTypeChange,
        onOutcomeTypeChange: onOutcomeTypeChange,
        onVitalChange: onVitalChange
    };
}());
