document.addEventListener('DOMContentLoaded', () => {
    fetchDbStatus();
    fetchFlowInfo();
});

function switchTab(tabId) {
    document.querySelectorAll('.tab-panel').forEach(panel => panel.classList.remove('active'));
    document.querySelectorAll('.tab-btn').forEach(btn => btn.classList.remove('active'));

    document.getElementById(tabId).classList.add('active');
    event.currentTarget.classList.add('active');
}

async function fetchDbStatus() {
    try {
        const res = await fetch('/api/comparison/status');
        const data = await res.json();

        const dbProviderEl = document.getElementById('dbProvider');
        const dbStatsEl = document.getElementById('dbStats');

        const isSqlServer = data.provider.includes('SqlServer');
        const badgeColor = isSqlServer ? '#10b981' : '#f59e0b';

        dbProviderEl.innerHTML = `Provider: <strong>${data.provider}</strong>`;
        dbStatsEl.innerHTML = `Data: <strong>${data.totalProducts.toLocaleString()}</strong> Products, <strong>${data.totalCategories}</strong> Categories`;
        
        document.querySelector('.status-dot').style.backgroundColor = badgeColor;
        document.querySelector('.status-dot').style.boxShadow = `0 0 10px ${badgeColor}`;
    } catch (err) {
        console.error('Error fetching DB status:', err);
    }
}

async function fetchFlowInfo() {
    try {
        const res = await fetch('/api/comparison/flow');
        const data = await res.json();

        renderFlowSteps('efFlowSteps', data.efCore.steps, 'ef');
        if (data.sqlCommand) {
            renderFlowSteps('sqlCommandFlowSteps', data.sqlCommand.steps, 'sql');
        }
        renderFlowSteps('dapperFlowSteps', data.dapper.steps, 'dapper');
    } catch (err) {
        console.error('Error fetching flow info:', err);
    }
}

function renderFlowSteps(containerId, steps, frameworkClass) {
    const container = document.getElementById(containerId);
    container.innerHTML = steps.map(step => `
        <div class="step-card ${frameworkClass}-step">
            <span class="step-number">Bước ${step.stepNumber}</span>
            <div class="step-title">${step.name}</div>
            <div class="step-desc">${step.description}</div>
            <div class="step-mechanism">
                <strong>Cơ chế ngầm:</strong> ${step.internalMechanism}
            </div>
            <div class="code-box" style="margin-top: 8px; font-size: 0.8rem;">${escapeHtml(step.codeSnippet)}</div>
        </div>
    `).join('');
}

async function runBenchmark() {
    const scenario = document.getElementById('scenarioSelect').value;
    const iterations = document.getElementById('iterationsInput').value;
    const isColdStart = document.getElementById('coldStartSelect').value === 'true';

    const runBtn = document.getElementById('runBtn');
    const btnText = document.getElementById('btnText');

    runBtn.disabled = true;
    btnText.innerHTML = `<span class="spinner"></span> Đang đo đạc...`;

    try {
        const res = await fetch(`/api/comparison/run-benchmark?scenario=${scenario}&iterations=${iterations}&isColdStart=${isColdStart}`);
        const data = await res.json();

        // Update EF Core
        document.getElementById('efTrackTime').textContent = `${data.efCoreTrackingResult.elapsedMilliseconds} ms`;
        document.getElementById('efTrackRam').textContent = formatBytes(data.efCoreTrackingResult.allocatedBytes);
        document.getElementById('efTrackCode').textContent = data.efCoreTrackingResult.codeSnippet;
        document.getElementById('efTrackSql').textContent = data.efCoreTrackingResult.sqlExecuted;

        // Update SQL Command (ADO.NET)
        document.getElementById('sqlCmdTime').textContent = `${data.sqlCommandResult.elapsedMilliseconds} ms`;
        document.getElementById('sqlCmdRam').textContent = formatBytes(data.sqlCommandResult.allocatedBytes);
        document.getElementById('sqlCmdCode').textContent = data.sqlCommandResult.codeSnippet;
        document.getElementById('sqlCmdSql').textContent = data.sqlCommandResult.sqlExecuted;

        // Update Dapper
        document.getElementById('dapperTime').textContent = `${data.dapperResult.elapsedMilliseconds} ms`;
        document.getElementById('dapperRam').textContent = formatBytes(data.dapperResult.allocatedBytes);
        document.getElementById('dapperCode').textContent = data.dapperResult.codeSnippet;
        document.getElementById('dapperSql').textContent = data.dapperResult.sqlExecuted;

        // Update Summary Banner
        document.getElementById('summaryBanner').innerHTML = `
            <span>${data.speedupSummary}</span>
        `;

    } catch (err) {
        console.error('Error running benchmark:', err);
        alert('Lỗi thực thi benchmark!');
    } finally {
        runBtn.disabled = false;
        btnText.textContent = '▶ Chạy Thử Nghiệm';
    }
}

function formatBytes(bytes) {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
}

function escapeHtml(str) {
    if (!str) return '';
    return str.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
}
