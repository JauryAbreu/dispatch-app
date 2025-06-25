document.addEventListener('DOMContentLoaded', () => {
    const leftTableBody = document.querySelector('#leftTable tbody');
    const rightTableBody = document.querySelector('#rightTable tbody');
    const btnToRight = document.getElementById('btnToRight');
    const btnToLeft = document.getElementById('btnToLeft');
    const btnComplete = document.getElementById('btnComplete');
    const btnCancel = document.getElementById('btnCancel');
    const btnChangeStatus = document.getElementById('btnChangeStatus');
    const searchInput = document.getElementById('searchInput');

    const binModalEl = document.getElementById('binModal');
    const binModal = bootstrap.Modal.getOrCreateInstance(binModalEl);
    const binInput = document.getElementById('binInput');
    const binError = document.getElementById('binError');
    const binSubmitBtn = document.getElementById('binSubmitBtn');

    const quantityModalEl = document.getElementById('quantityModal');
    const quantityModal = bootstrap.Modal.getOrCreateInstance(quantityModalEl);
    const quantityInput = document.getElementById('quantityInput');
    const quantityError = document.getElementById('quantityError');
    const quantitySubmitBtn = document.getElementById('quantitySubmitBtn');
    const maxQuantityLabel = document.getElementById('maxQuantityLabel');

    const cancelModalEl = document.getElementById('cancelModal');
    const cancelModal = bootstrap.Modal.getOrCreateInstance(cancelModalEl);
    const cancelConfirmBtn = document.getElementById('cancelConfirmBtn');

    const completeModalEl = document.getElementById('completeModal');
    const completeModal = bootstrap.Modal.getOrCreateInstance(completeModalEl);
    const completeConfirmBtn = document.getElementById('completeConfirmBtn');

    // Selected state tracking
    let selectedLeftRow = null;
    const selectedRightRows = new Set();
    let pendingBIN = '';

    // Utility functions
    function updateButtonsState() {
        btnToRight.disabled = !selectedLeftRow;
        btnToRight.setAttribute('aria-disabled', btnToRight.disabled ? 'true' : 'false');

        const rightRowCount = rightTableBody.querySelectorAll('tr').length;
        const rightSelected = selectedRightRows.size > 0;
        btnToLeft.disabled = !rightSelected;
        btnChangeStatus.disabled = !rightSelected;
        btnComplete.disabled = rightRowCount === 0;

        btnToLeft.setAttribute('aria-disabled', btnToLeft.disabled ? 'true' : 'false');
        btnChangeStatus.setAttribute('aria-disabled', btnChangeStatus.disabled ? 'true' : 'false');
        btnComplete.setAttribute('aria-disabled', btnComplete.disabled ? 'true' : 'false');
    }

    function clearLeftSelection() {
        leftTableBody.querySelectorAll('tr.selected').forEach(r => {
            r.classList.remove('selected');
            r.setAttribute('aria-selected', 'false');
        });
        selectedLeftRow = null;
    }

    function clearRightSelection() {
        selectedRightRows.clear();
        rightTableBody.querySelectorAll('tr.selected').forEach(r => {
            r.classList.remove('selected');
            r.setAttribute('aria-selected', 'false');
        });
    }

    // Left row click - single selection
    function onLeftRowClick(e) {
        const tr = e.currentTarget;
        if (selectedLeftRow && selectedLeftRow !== tr) clearLeftSelection();
        const isSelected = tr.classList.toggle('selected');
        tr.setAttribute('aria-selected', isSelected ? 'true' : 'false');
        selectedLeftRow = isSelected ? tr : null;
        updateButtonsState();
    }
    function onLeftRowKeyDown(e) {
        if (e.key === ' ' || e.key === 'Enter') {
            e.preventDefault();
            onLeftRowClick(e);
        }
    }
    function addLeftRowListeners() {
        leftTableBody.querySelectorAll('tr').forEach(row => {
            row.removeEventListener('click', onLeftRowClick);
            row.removeEventListener('keydown', onLeftRowKeyDown);
            row.addEventListener('click', onLeftRowClick);
            row.addEventListener('keydown', onLeftRowKeyDown);
        });
    }

    // Right row click - multi selection with ctrl/cmd
    function onRightRowClick(e) {
        const tr = e.currentTarget;
        const isSelected = tr.classList.contains('selected');
        if (e.ctrlKey || e.metaKey) {
            if (isSelected) {
                tr.classList.remove('selected');
                tr.setAttribute('aria-selected', 'false');
                selectedRightRows.delete(tr);
            } else {
                tr.classList.add('selected');
                tr.setAttribute('aria-selected', 'true');
                selectedRightRows.add(tr);
            }
        } else {
            clearRightSelection();
            tr.classList.add('selected');
            tr.setAttribute('aria-selected', 'true');
            selectedRightRows.add(tr);
        }
        updateButtonsState();
    }
    function onRightRowKeyDown(e) {
        if (e.key === ' ' || e.key === 'Enter') {
            e.preventDefault();
            onRightRowClick(e);
        }
    }
    function addRightRowListeners() {
        rightTableBody.querySelectorAll('tr').forEach(row => {
            row.removeEventListener('click', onRightRowClick);
            row.removeEventListener('keydown', onRightRowKeyDown);
            row.addEventListener('click', onRightRowClick);
            row.addEventListener('keydown', onRightRowKeyDown);
        });
    }

    // Filter left table by search
    function filterLeftTable() {
        const filter = searchInput.value.trim().toLowerCase();
        let exactMatch = null;
        leftTableBody.querySelectorAll('tr').forEach(row => {
            const code = row.dataset.code.toLowerCase();
            const barcode = row.dataset.barcode.toLowerCase();
            const desc = row.dataset.description.toLowerCase();
            const match = code.includes(filter) || desc.includes(filter) || barcode.includes(filter);
            row.style.display = match ? '' : 'none';
            if (match && (code === filter || desc === filter || barcode === filter)) exactMatch = row;
        });
        return exactMatch;
    }
    searchInput.addEventListener('input', () => {
        clearLeftSelection();
        filterLeftTable();
        updateButtonsState();
    });
    searchInput.addEventListener('keydown', e => {
        if (e.key === 'Enter') {
            e.preventDefault();
            const exact = filterLeftTable();
            if (!exact) {
                showToast('No se encontró un artículo disponible con ese código o descripción.', 'danger');
                return;
            }
            clearLeftSelection();
            exact.classList.add('selected');
            exact.setAttribute('aria-selected', 'true');
            selectedLeftRow = exact;
            updateButtonsState();
            moveLeftRowToRight();
        }
    });

    // Move left row to right - show modals for BIN and quantity
    function moveLeftRowToRight() {
        if (!selectedLeftRow) return;
        if (parseInt(selectedLeftRow.dataset.pendiente, 10) <= 0) {
            alert('Este artículo no tiene cantidad pendiente para transferir.');
            return;
        }
        binInput.value = '';
        binError.textContent = '';
        binInput.classList.remove('is-invalid');
        binModal.show();
        setTimeout(() => binInput.focus(), 310);
    }
    binSubmitBtn.addEventListener('click', () => {
        const val = binInput.value.trim();
        if (!val) {
            binError.textContent = 'El campo BIN es obligatorio.';
            binInput.classList.add('is-invalid');
            binInput.focus();
            return;
        }
        binError.textContent = '';
        binInput.classList.remove('is-invalid');
        pendingBIN = val;
        binModal.hide();
        maxQuantityLabel.textContent = selectedLeftRow.dataset.pendiente;
        quantityInput.value = '';
        quantityError.textContent = '';
        quantityInput.classList.remove('is-invalid');
        quantityModal.show();
        setTimeout(() => quantityInput.focus(), 310);
    });
    quantitySubmitBtn.addEventListener('click', () => {
        const valStr = quantityInput.value.trim();
        const valNum = Number(valStr);
        const pendiente = parseInt(selectedLeftRow.dataset.pendiente, 10);
        if (!valStr || isNaN(valNum) || valNum < 1) {
            quantityError.textContent = 'Cantidad inválida. Debe ser un número entero mayor que cero.';
            quantityInput.classList.add('is-invalid');
            quantityInput.focus();
            return;
        }
        if (valNum > pendiente) {
            quantityError.textContent = 'No puede transferir más que la cantidad pendiente.';
            quantityInput.classList.add('is-invalid');
            quantityInput.focus();
            return;
        }
        quantityError.textContent = '';
        quantityInput.classList.remove('is-invalid');
        quantityModal.hide();
        updateLeftRowPendingTransfer(selectedLeftRow, valNum);
        addOrUpdateRightRow(
            selectedLeftRow.dataset.code,
            selectedLeftRow.dataset.barcode,
            selectedLeftRow.dataset.description,
            valNum,
            pendingBIN,
            1
        );
        clearLeftSelection();
        updateButtonsState();
        searchInput.value = '';
        filterLeftTable();
        showToast(`Se transfirieron ${valNum} unidades del artículo ${selectedLeftRow.dataset.code} al BIN "${pendingBIN}".`, 'success');
    });

    // Add or update right table row, estado: 1=Pendiente_Despacho ("Entrega"), 0=Pendiente_Entrega ("Domicilio")
    function addOrUpdateRightRow(code, barcode, description, quantity, bin, estado) {
        const rows = rightTableBody.querySelectorAll('tr');
        for (const row of rows) {
            if (row.dataset.code === code && row.dataset.bin.toLowerCase() === bin.toLowerCase() && row.dataset.estado == 1) {
                let curQty = parseInt(row.dataset.quantity, 10);
                curQty += quantity;
                row.dataset.quantity = curQty;
                row.children[2].textContent = curQty;
                // Update estado if different
                if (row.dataset.estado != estado) {
                    row.dataset.estado = estado;
                    row.children[4].textContent = estado === 0 ? 'Domicilio' : 'Entrega';
                }
                return;
            }
        }
        const tr = document.createElement('tr');
        tr.tabIndex = 0;
        tr.setAttribute('aria-selected', 'false');
        tr.dataset.code = code;
        tr.dataset.barcode = barcode;
        tr.dataset.description = description;
        tr.dataset.quantity = quantity;
        tr.dataset.bin = bin;
        tr.dataset.estado = estado;
        tr.innerHTML = `
              <td>${code}</td>
              <td>${barcode}</td>
              <td>${description}</td>
              <td>${quantity}</td>
              <td>${bin}</td>
              <td>${estado === 0 ? 'Domicilio' : 'Entrega'}</td>
            `;
        rightTableBody.appendChild(tr);
        addRightRowListeners();
    }

    // Update left row Pendiente and Transferido
    function updateLeftRowPendingTransfer(row, transferQty) {
        let pendiente = parseInt(row.dataset.pendiente, 10);
        let transferido = parseInt(row.dataset.transferido, 10);
        pendiente = Math.max(0, pendiente - transferQty);
        transferido += transferQty;
        row.dataset.pendiente = pendiente;
        row.dataset.transferido = transferido;
        row.children[4].textContent = pendiente;
        row.children[5].textContent = transferido;
    }

    // Button: Enviar a la derecha
    btnToRight.addEventListener('click', () => {
        if (!selectedLeftRow) return;
        moveLeftRowToRight();
    });

    // Button: Enviar a la izquierda (from right to left)
    btnToLeft.addEventListener('click', () => {
        if (selectedRightRows.size === 0) return;
        selectedRightRows.forEach(row => {
            const code = row.dataset.code;
            const qty = parseInt(row.dataset.quantity, 10);
            const leftRow = [...leftTableBody.querySelectorAll('tr')].find(r => r.dataset.code === code);
            if (leftRow) {
                let pendiente = parseInt(leftRow.dataset.pendiente, 10);
                let transferido = parseInt(leftRow.dataset.transferido, 10);
                pendiente += qty;
                transferido -= qty;
                if (transferido < 0) transferido = 0;
                leftRow.dataset.pendiente = pendiente;
                leftRow.dataset.transferido = transferido;
                leftRow.children[4].textContent = pendiente;
                leftRow.children[5].textContent = transferido;
            }
            rightTableBody.removeChild(row);
            selectedRightRows.delete(row);
        });
        updateButtonsState();
        showToast('Artículo(s) enviado(s) de vuelta a disponibles.', 'info');
    });

    // Button: Cambiar Estado toggles entre 0 y 1 para estado de artículos seleccionados
    btnChangeStatus.addEventListener('click', () => {
        if (selectedRightRows.size === 0) return;
        selectedRightRows.forEach(row => {
            const currentEstado = parseInt(row.dataset.estado, 10);
            const newEstado = currentEstado === 0 ? 1 : 0;
            row.dataset.estado = newEstado;
            row.children[5].textContent = newEstado === 0 ? 'Domicilio' : 'Entrega';
        });
        showToast('Estados de artículos seleccionados actualizados.', 'success');
    });

    // Button: Cancelar Proceso modal and confirm
    btnCancel.addEventListener('click', () => {
        cancelModal.show();
    });
    cancelConfirmBtn.addEventListener('click', () => {
        cancelModal.hide();
        window.location.href = '/Home/Index';
    });

    // Button: Completar Proceso modal and confirm
    btnComplete.addEventListener('click', () => {
        completeModal.show();
    });
    completeConfirmBtn.addEventListener('click', async () => {
        completeModal.hide();
        const rows = rightTableBody.querySelectorAll('tr');
        if (rows.length === 0) {
            showToast('No hay artículos en despacho para enviar.', 'warning');
            return;
        }
        const dataToSend = [];
        rows.forEach(row => {
            dataToSend.push({
                Sku: row.dataset.code,
                Barcode: row.dataset.barcode,
                Description: row.dataset.description,
                Quantity: parseInt(row.dataset.quantity, 10),
                Bin: row.dataset.bin,
                Status: parseInt(row.dataset.estado, 10) // Send enum value to server
            });
        });
        console.log(dataToSend)
        const id = document.getElementById("headerId").value;
        console.log(id);
        try {
            btnComplete.disabled = true;
            btnComplete.textContent = 'Enviando...';
            const response = await fetch('Create', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ id: id, lines: dataToSend })
            });
            if (!response.ok) throw new Error('Error en la respuesta del servidor.');
            rightTableBody.innerHTML = '';
            selectedRightRows.clear();
            updateButtonsState();
            showToast('Proceso completado con éxito.', 'success');
            window.location.href = '/Transactions/Create?id=' + id;
        } catch (error) {
            showToast('Error al completar el proceso: ' + error.message, 'danger');
        } finally {
            btnComplete.disabled = false;
            btnComplete.textContent = 'Completar Proceso';
        }
    });

    function showToast(message, type = 'info') {
        const toastContainer = document.getElementById('toastContainer');
        const toastId = 'toast-' + Math.random().toString(36).substr(2, 10);
        const bgClass = {
            success: 'bg-success text-white',
            danger: 'bg-danger text-white',
            warning: 'bg-warning text-dark',
            info: 'bg-info text-dark'
        }[type] || 'bg-info text-dark';
        const toastElem = document.createElement('div');
        toastElem.className = `toast align-items-center text-white ${bgClass} border-0`;
        toastElem.id = toastId;
        toastElem.role = 'alert';
        toastElem.ariaLive = 'assertive';
        toastElem.ariaAtomic = 'true';
        toastElem.style.minWidth = '280px';
        toastElem.innerHTML = `
              <div class="d-flex">
                <div class="toast-body">${message}</div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Cerrar"></button>
              </div>
            `;
        toastContainer.appendChild(toastElem);
        const toastBootstrap = new bootstrap.Toast(toastElem, { delay: 5000 });
        toastBootstrap.show();
        toastElem.addEventListener('hidden.bs.toast', () => {
            toastElem.remove();
        });
    }

    addLeftRowListeners();
    function addRightRowListeners() {
        rightTableBody.querySelectorAll('tr').forEach(row => {
            row.removeEventListener('click', onRightRowClick);
            row.removeEventListener('keydown', onRightRowKeyDown);
            row.addEventListener('click', onRightRowClick);
            row.addEventListener('keydown', onRightRowKeyDown);
        });
    }
    function onRightRowClick(e) {
        const tr = e.currentTarget;
        if (e.ctrlKey || e.metaKey) {
            if (tr.classList.contains('selected')) {
                tr.classList.remove('selected');
                tr.setAttribute('aria-selected', 'false');
                selectedRightRows.delete(tr);
            } else {
                tr.classList.add('selected');
                tr.setAttribute('aria-selected', 'true');
                selectedRightRows.add(tr);
            }
        } else {
            selectedRightRows.forEach(r => {
                r.classList.remove('selected');
                r.setAttribute('aria-selected', 'false');
            });
            selectedRightRows.clear();
            tr.classList.add('selected');
            tr.setAttribute('aria-selected', 'true');
            selectedRightRows.add(tr);
        }
        updateButtonsState();
    }
    function onRightRowKeyDown(e) {
        if (e.key === ' ' || e.key === 'Enter') {
            e.preventDefault();
            onRightRowClick(e);
        }
    }
    addRightRowListeners();
    updateButtonsState();
});