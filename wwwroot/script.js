(() => {
  'use strict';

  const $ = (sel, parent = document) => parent.querySelector(sel);
  const $$ = (sel, parent = document) => [...parent.querySelectorAll(sel)];

  const BRAND = 'Sistema White Label';

  // Usa o mesmo endereço pelo qual a página foi aberta (localhost, IP da VPN, etc.)
  const API = window.location.origin + '/api';

  const money = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });
  const fmt = (v) => money.format(Number(v) || 0);

  const esc = (v) =>
    String(v ?? '').replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));

  const onlyDigits = (v) => String(v ?? '').replace(/\D/g, '');

  const formatCpf = (v) => {
    const d = onlyDigits(v);
    return d.length === 11 ? `${d.slice(0, 3)}.${d.slice(3, 6)}.${d.slice(6, 9)}-${d.slice(9)}` : (v || '—');
  };

  /* ------------------------------ sessão ------------------------------ */
  // Chaves do localStorage: accessToken, username, userRole, userTipo, tokenExpiresAt
  const Session = {
    get token() { return localStorage.getItem('accessToken'); },
    get username() { return localStorage.getItem('username') || ''; },
    get role() { return localStorage.getItem('userRole') || ''; },
    get expiresAt() { return localStorage.getItem('tokenExpiresAt'); },

    get valid() {
      if (!this.token) return false;
      return !this.expiresAt || new Date(this.expiresAt) > new Date();
    },

    save(data) {
      localStorage.setItem('accessToken', data.accessToken);
      localStorage.setItem('username', data.username);
      localStorage.setItem('userRole', data.role);
      localStorage.setItem('userTipo', data.tipo);
      localStorage.setItem('tokenExpiresAt', data.expiresAt);
    },

    clear() {
      ['accessToken', 'refreshToken', 'username', 'userRole', 'userTipo', 'tokenExpiresAt']
        .forEach((k) => localStorage.removeItem(k));
    }
  };

  const Prefs = {
    get theme() {
      return localStorage.getItem('app_theme') || 'light';
    },
    set theme(v) {
      localStorage.setItem('app_theme', v);
    }
  };

  // Perfis (tbl_Perfil). Vendedor só cadastra clientes; o resto é do Administrador e do Gerente.
  const MANAGERS = ['Administrador', 'Gerente'];
  const isManager = () => MANAGERS.includes(Session.role);

  /* -------------------------------- API -------------------------------- */
  // Todas as chamadas passam aqui: coloca o token, trata sessão vencida (401) e devolve o erro em texto
  async function api(path, { method = 'GET', body, headers = {}, noAuth = false, raw = false } = {}) {
    const finalHeaders = { ...headers };
    if (body !== undefined) finalHeaders['Content-Type'] = 'application/json';
    if (!noAuth && Session.token) finalHeaders['Authorization'] = `Bearer ${Session.token}`;

    const response = await fetch(API + path, {
      method,
      headers: finalHeaders,
      body: body !== undefined ? JSON.stringify(body) : undefined
    });

    // 401 fora do login = sessão vencida (login e 2FA tratam o próprio 401: senha ou código errado)
    if (response.status === 401 && !noAuth) {
      Session.clear();
      sessionStorage.setItem('flash', 'Sua sessão terminou. Entre novamente.');
      location.href = 'login.html';
      throw new Error('Sessão expirada. Faça login novamente.');
    }

    if (!response.ok) {
      let message = `Erro ${response.status}`;
      if (response.status === 403) message = 'Seu perfil não tem permissão para esta ação.';
      if (response.status === 404) message = 'Não encontrado (o módulo pode estar desativado).';

      try {
        const data = await response.json();
        message = data.mensagem || data.title || message;
      } catch { /* resposta sem corpo */ }

      throw new Error(message);
    }

    if (raw) return response;
    if (response.status === 204) return null;

    const type = response.headers.get('content-type') || '';
    return type.includes('json') ? response.json() : null;
  }

  // Baixa um PDF (comprovante de venda / recibo de pagamento) usando o token da sessão
  async function downloadPdf(path, fileName) {
    const response = await api(path, { raw: true });
    const blob = await response.blob();
    const url = URL.createObjectURL(blob);

    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    a.remove();
    URL.revokeObjectURL(url);
  }

  /* ------------------------------ datas ------------------------------ */
  function isoDate(date = new Date()) {
    const y = date.getFullYear();
    const m = String(date.getMonth() + 1).padStart(2, '0');
    const d = String(date.getDate()).padStart(2, '0');
    return `${y}-${m}-${d}`;
  }

  function displayDate(value) {
    if (!value) return '—';
    const [y, m, d] = String(value).slice(0, 10).split('-');
    return `${d}/${m}/${y}`;
  }

  function filterByRange(sales, range) {
    const today = new Date();
    const end = isoDate(today);
    const start = new Date(today);

    if (range === 'today') return sales.filter((s) => s.date === end);

    if (range === 'prev') {
      const from = isoDate(new Date(today.getFullYear(), today.getMonth() - 1, 1));
      const to = isoDate(new Date(today.getFullYear(), today.getMonth(), 0));
      return sales.filter((s) => s.date >= from && s.date <= to);
    }

    if (range === '7') start.setDate(today.getDate() - 6);
    if (range === '30') start.setDate(today.getDate() - 29);
    if (range === 'month') start.setDate(1);

    const from = isoDate(start);
    return sales.filter((s) => s.date >= from && s.date <= end);
  }

  /* --------------------------- módulos / menu --------------------------- */
  // Módulos ligados no banco (tbl_Modulo): { fiado: true, ... }
  let modules = {};
  const moduleOn = (name) => !!modules[name];

  async function loadModules() {
    try {
      const list = await api('/Modulos');
      modules = {};
      list.forEach((m) => (modules[m.nome] = m.ativo));
    } catch {
      modules = {};
    }
  }

  // roles = quem pode ver; module = só aparece com o módulo ligado
  const menu = [
    { href: 'index.html', label: 'Dashboard' },
    { href: 'vendas.html', label: 'Nova venda' },
    { href: 'historico.html', label: 'Histórico de vendas' },
    { href: 'fiados.html', label: 'Fiados', module: 'fiado' },
    { href: 'produtos.html', label: 'Produtos', roles: MANAGERS },
    { href: 'estoque.html', label: 'Estoque', roles: MANAGERS },
    { href: 'clientes.html', label: 'Clientes' },
    { href: 'cadastro.html', label: 'Cadastros', roles: MANAGERS },
    { href: 'configuracoes.html', label: 'Configurações' }
  ];

  const canSee = (item) =>
    (!item.roles || item.roles.includes(Session.role)) && (!item.module || moduleOn(item.module));

  function currentPage() {
    return location.pathname.split('/').pop() || 'index.html';
  }

  // Quem não tem perfil ou módulo para a página volta para o início
  function guardPage() {
    const item = menu.find((m) => m.href === currentPage());
    if (item && !canSee(item)) {
      sessionStorage.setItem('flash', item.module && !moduleOn(item.module)
        ? 'Este módulo está desativado.'
        : 'Seu perfil não tem acesso a esta página.');
      location.href = 'index.html';
      return false;
    }
    return true;
  }

  function applyTheme() {
    document.documentElement.dataset.theme = Prefs.theme;
  }

  function buildShell() {
    applyTheme();
    if ($('.header')) return;

    const current = currentPage();

    document.body.insertAdjacentHTML('afterbegin', `
      <header class="header">
        <div class="header-inner">
          <button class="menu-btn" id="menuBtn" aria-label="Abrir menu">☰</button>
          <div class="brand">${BRAND}</div>
        </div>
      </header>
      <div class="drawer-backdrop" id="drawerBackdrop"></div>
      <aside class="drawer" id="drawer">
        <div class="drawer-head">
          <strong>${BRAND}</strong>
          <button class="close-btn" id="closeDrawer" aria-label="Fechar menu">×</button>
        </div>
        <nav class="drawer-nav">
          ${menu.filter(canSee).map((item) =>
            `<a class="nav-link ${current === item.href ? 'active' : ''}" href="${item.href}">${item.label}</a>`
          ).join('')}
        </nav>
        <div class="drawer-foot">
          <div>${esc(Session.username)} · ${esc(Session.role)}</div>
          <button class="btn btn-secondary" id="drawerLogout" style="margin-top:10px;width:100%">Sair</button>
        </div>
      </aside>
      <div class="toast" id="toast"></div>
    `);

    const drawer = $('#drawer');
    const backdrop = $('#drawerBackdrop');
    const setOpen = (open) => {
      drawer.classList.toggle('open', open);
      backdrop.classList.toggle('open', open);
    };

    $('#menuBtn').addEventListener('click', () => setOpen(true));
    $('#closeDrawer').addEventListener('click', () => setOpen(false));
    backdrop.addEventListener('click', () => setOpen(false));
    $('#drawerLogout').addEventListener('click', logout);
    document.addEventListener('keydown', (e) => e.key === 'Escape' && setOpen(false));
  }

  function logout() {
    // O servidor não guarda a sessão: basta descartar o token (a chamada é só um aviso)
    if (Session.token) api('/Auth/logout', { method: 'POST' }).catch(() => {});
    Session.clear();
    location.href = 'login.html';
  }

  let toastTimer;
  function toast(message) {
    const el = $('#toast');
    if (!el) return;
    el.textContent = message;
    el.classList.add('show');
    clearTimeout(toastTimer);
    toastTimer = setTimeout(() => el.classList.remove('show'), 3200);
  }

  // Roda uma tela e mostra o erro (em vez de quebrar a página em silêncio)
  const run = (fn) => fn().catch((error) => {
    console.error(error);
    toast(error.message || 'Erro inesperado.');
  });

  function bindModal(modalId, openBtnId, closeIds) {
    const modal = $(modalId);
    if (openBtnId) $(openBtnId).addEventListener('click', () => modal.classList.add('open'));
    closeIds.forEach((id) => $(id).addEventListener('click', () => modal.classList.remove('open')));
    return modal;
  }

  /* ------------------------------- dados ------------------------------- */
  // O que a API devolve -> o formato que as telas usam
  const toProduct = (p) => ({
    id: p.idProduto,
    name: p.descricao,
    categoryId: p.idCategoria,
    categoryName: p.categoriaDescricao || 'Sem categoria',
    cost: p.precoCompra,
    price: p.precoVenda,
    stock: p.qtde,
    minStock: p.estoqueMinimo,
    unitId: p.idUnidadeMedida
  });

  const toCustomer = (c) => ({
    id: c.idPessoa,
    name: c.nome,
    cpf: c.cpf,
    phone: c.telefone === 'Não informado' ? '' : (c.telefone || ''),
    address: c.endereco === 'Não informado' ? '' : (c.endereco || ''),
    notes: c.observacao || '',
    purchases: c.quantidadeCompras || 0,
    totalBought: c.totalComprado || 0,
    fiado: c.totalFiado || 0
  });

  const toSale = (s) => ({
    id: s.idVenda,
    date: String(s.dataHora).slice(0, 10),
    time: String(s.dataHora).slice(11, 16),
    customerId: s.idCliente,
    customerName: s.idCliente ? s.nomeCliente : '',
    payment: s.formaPagamento,
    isFiado: s.isFiado,
    discount: s.totalDesconto,
    total: s.totalVenda,
    gross: s.totalBruto,
    items: (s.itens || []).map((i) => ({
      productId: i.produtoId,
      name: i.nomeProduto,
      qty: i.quantidade,
      price: i.precoUnitario,
      discount: i.valorDesconto,
      total: i.valorTotal
    }))
  });

  const loadProducts = async () => (await api('/Produto')).map(toProduct);
  const loadCustomers = async () => (await api('/Cliente')).map(toCustomer);
  const loadCategories = async () => (await api('/Categoria')).map((c) => ({ id: c.idCategoria, name: c.descricao }));
  const loadSales = async () => (await api('/Venda')).map(toSale);

  // Formas de pagamento da tela (têm que existir em tbl_Tipo_Pagamento). Fiado só com o módulo ligado.
  const PAYMENTS = ['Pix', 'Dinheiro', 'Cartão'];
  const paymentOptions = () => (moduleOn('fiado') ? [...PAYMENTS, 'Fiado'] : PAYMENTS);

  const stockStatus = (p) => {
    if (p.stock <= 0) return { cls: 'bad', label: 'Sem estoque' };
    if (p.stock <= p.minStock) return { cls: 'warn', label: 'Estoque baixo' };
    return { cls: 'good', label: 'Disponível' };
  };

  /* ----------------------------- gráficos ----------------------------- */
  // Linha cheia = líquido (com desconto), tracejada = bruto (sem desconto)
  function drawLineChart(container, days) {
    if (!container) return;

    if (!days.length) {
      container.innerHTML = '<div class="empty">Sem vendas neste período.</div>';
      return;
    }

    const W = 1000, H = 250, padX = 44, padY = 26;
    const max = Math.max(...days.map((d) => d.bruto), 1);

    const pointsOf = (key) => days.map((d, i) => [
      padX + (i / Math.max(days.length - 1, 1)) * (W - 80),
      H - padY - (d[key] / max) * (H - 60),
      d
    ]);

    const net = pointsOf('liquido');
    const gross = pointsOf('bruto');
    const pathOf = (points) => points.map(([x, y], i) => `${i ? 'L' : 'M'} ${x} ${y}`).join(' ');

    const guides = [0.25, 0.5, 0.75, 1].map((lvl) => {
      const y = H - padY - lvl * (H - 60);
      return `<line x1="${padX}" x2="${W - 24}" y1="${y}" y2="${y}" stroke="var(--line)" stroke-dasharray="4 5"/>`;
    }).join('');

    container.innerHTML = `
      <svg viewBox="0 0 ${W} ${H}" aria-label="Faturamento por dia">
        ${guides}
        <path d="${pathOf(gross)}" fill="none" stroke="var(--muted)" stroke-width="3" stroke-dasharray="8 7" stroke-linecap="round" stroke-linejoin="round"/>
        <path d="${pathOf(net)}" fill="none" stroke="var(--green)" stroke-width="4" stroke-linecap="round" stroke-linejoin="round"/>
        ${net.map(([x, y, d]) =>
          `<circle cx="${x}" cy="${y}" r="5" fill="var(--green)"><title>${displayDate(d.data)} - líquido ${fmt(d.liquido)} · bruto ${fmt(d.bruto)}</title></circle>`
        ).join('')}
      </svg>
      <div class="muted" style="font-size:12px;margin-top:6px">Linha verde: líquido (com desconto) · tracejada: bruto (sem desconto)</div>`;
  }

  // rows: { label, value, sub } (value define o tamanho da barra, sub é o texto pequeno ao lado)
  function drawBars(container, rows, emptyText, format = (v) => v) {
    if (!container) return;

    if (!rows.length) {
      container.innerHTML = `<div class="empty">${emptyText}</div>`;
      return;
    }

    const max = Math.max(...rows.map((r) => r.value), 1);
    container.innerHTML = rows.slice(0, 6).map((r) => `
      <div style="display:grid;grid-template-columns:150px 1fr auto;gap:10px;align-items:center;margin:14px 0">
        <div class="muted" style="overflow:hidden;text-overflow:ellipsis;white-space:nowrap">${esc(r.label)}</div>
        <div style="height:12px;border-radius:999px;background:var(--surface-2);overflow:hidden">
          <div style="height:100%;width:${(r.value / max) * 100}%;background:var(--green);border-radius:999px"></div>
        </div>
        <div style="text-align:right"><strong>${format(r.value)}</strong>${r.sub ? `<div class="muted" style="font-size:11px">${r.sub}</div>` : ''}</div>
      </div>`).join('');
  }

  /* ------------------------------ telas ------------------------------ */
  async function initDashboard() {
    if (!$('#dashboardPage')) return;

    let range = '30';
    const empty = 'Sem vendas neste período.';

    async function update() {
      const d = await api(`/Dashboard?periodo=${range}`);

      $('#metricSales').textContent = d.quantidadeVendas.toLocaleString('pt-BR');
      $('#metricNet').textContent = fmt(d.faturamentoLiquido);
      $('#metricGross').textContent = fmt(d.faturamentoBruto);
      $('#metricGrossFoot').textContent = `Descontos: ${fmt(d.totalDescontos)}`;
      $('#metricProducts').textContent = d.produtosVendidos.toLocaleString('pt-BR');
      $('#metricTicket').textContent = fmt(d.ticketMedioLiquido);
      $('#metricTicketFoot').textContent = `Bruto: ${fmt(d.ticketMedioBruto)}`;
      $('#metricStock').textContent = d.produtosSemEstoque.toLocaleString('pt-BR');
      $('#metricLow').textContent = d.produtosBaixoEstoque.toLocaleString('pt-BR');

      // Cards do fiado só aparecem com o módulo ligado
      $$('[data-module-card="fiado"]').forEach((el) => (el.style.display = d.fiadoAtivo ? '' : 'none'));
      if (d.fiadoAtivo) {
        $('#metricFiado').textContent = fmt(d.totalFiado);
        $('#metricFiadoFoot').textContent = `Vendido no fiado no período: ${fmt(d.vendasFiadasPeriodo)}`;
      }

      drawLineChart($('#salesChart'), d.vendasPorDia);

      drawBars($('#topProducts'),
        [...d.porProduto].sort((a, b) => b.quantidade - a.quantidade)
          .map((r) => ({ label: r.nome, value: r.quantidade })), empty);

      drawBars($('#revenueProducts'),
        [...d.porProduto].sort((a, b) => b.liquido - a.liquido)
          .map((r) => ({ label: r.nome, value: r.liquido, sub: `bruto ${fmt(r.bruto)}` })), empty, fmt);

      const categories = [...d.porCategoria].sort((a, b) => b.liquido - a.liquido);
      const maxCat = Math.max(...categories.map((c) => c.liquido), 1);
      $('#categorySales').innerHTML = categories.length ? categories.map((c) => `
        <div style="margin:14px 0">
          <div style="display:flex;justify-content:space-between;gap:10px;margin-bottom:7px"><span>${esc(c.nome)}</span><span><strong>${fmt(c.liquido)}</strong> <span class="muted">· bruto ${fmt(c.bruto)}</span></span></div>
          <div style="height:9px;background:var(--surface-2);border-radius:999px;overflow:hidden"><div style="height:100%;width:${(c.liquido / maxCat) * 100}%;background:var(--green);border-radius:999px"></div></div>
        </div>`).join('') : `<div class="empty">${empty}</div>`;

      $('#paymentSales').innerHTML = d.porPagamento.length ? d.porPagamento.map((p) => `
        <div style="display:flex;justify-content:space-between;gap:16px;padding:10px 0;border-bottom:1px solid var(--line)">
          <span>${esc(p.forma)} <span class="muted">· ${p.quantidade} venda${p.quantidade === 1 ? '' : 's'}</span></span>
          <span><strong>${fmt(p.liquido)}</strong> <span class="muted">· bruto ${fmt(p.bruto)}</span></span>
        </div>`).join('') : `<div class="empty">${empty}</div>`;
    }

    $$('.segment[data-range]').forEach((btn) => btn.addEventListener('click', () => {
      $$('.segment[data-range]').forEach((b) => b.classList.remove('active'));
      btn.classList.add('active');
      range = btn.dataset.range;
      run(update);
    }));

    await update();
  }

  // Corpo enviado ao criar/editar produto (o ID do produto faz o papel do antigo SKU)
  const productBody = (p) => ({
    descricao: p.name,
    precoCompra: p.cost,
    precoVenda: p.price,
    qtde: p.stock,
    idCategoria: p.categoryId,
    idUnidadeMedida: p.unitId || 1,
    estoqueMinimo: p.minStock
  });

  async function initProducts() {
    if (!$('#productsPage')) return;

    const tbody = $('#productsBody');
    const categoryFilter = $('#productCategoryFilter');
    const search = $('#productSearch');
    const modal = bindModal('#productModal', null, ['#closeProductModal', '#cancelProduct']);

    let products = await loadProducts();
    const categories = await loadCategories();

    const options = categories.map((c) => `<option value="${c.id}">${esc(c.name)}</option>`).join('');
    categoryFilter.innerHTML = '<option value="">Todas as categorias</option>' + options;
    $('#productCategory').innerHTML = options;

    const geral = categories.find((c) => c.name.toLowerCase() === 'geral');

    function draw() {
      const term = search.value.trim().toLowerCase();
      const categoryId = categoryFilter.value;

      const filtered = products.filter((p) => {
        const text = !term || p.name.toLowerCase().includes(term) || String(p.id) === term;
        const cat = !categoryId || String(p.categoryId) === String(categoryId);
        return text && cat;
      });

      const n = products.length;
      $('#productCount').textContent = `${n} produto${n === 1 ? '' : 's'} cadastrado${n === 1 ? '' : 's'}`;

      tbody.innerHTML = filtered.length ? filtered.map((p) => {
        const profit = p.price - p.cost;
        const margin = p.price ? (profit / p.price) * 100 : 0;
        const st = stockStatus(p);
        return `
          <tr>
            <td class="td-strong">#${p.id}</td>
            <td><strong>${esc(p.name)}</strong></td>
            <td>${esc(p.categoryName)}</td>
            <td>${fmt(p.cost)}</td>
            <td><strong>${fmt(p.price)}</strong></td>
            <td>${margin.toFixed(1)}%</td>
            <td>${fmt(profit)}</td>
            <td>${p.stock}</td>
            <td>${p.minStock}</td>
            <td><span class="status ${st.cls}">${st.label}</span></td>
            <td>
              <button class="btn btn-secondary" data-edit-product="${p.id}">Editar</button>
              <button class="btn btn-danger" data-delete-product="${p.id}">Excluir</button>
            </td>
          </tr>`;
      }).join('') : '<tr><td colspan="11"><div class="empty">Nenhum produto cadastrado.</div></td></tr>';
    }

    function openForm(id = null) {
      const product = id ? products.find((p) => String(p.id) === String(id)) : null;
      $('#productModalTitle').textContent = product ? `Editar produto #${product.id}` : 'Novo produto';
      $('#productId').value = product?.id || '';
      $('#productName').value = product?.name || '';
      $('#productCategory').value = product?.categoryId || geral?.id || '';
      $('#productCost').value = product?.cost ?? '';
      $('#productPrice').value = product?.price ?? '';
      $('#productStock').value = product?.stock ?? 0;
      $('#productMin').value = product?.minStock ?? 10;
      modal.classList.add('open');
    }

    tbody.addEventListener('click', (e) => {
      const edit = e.target.closest('[data-edit-product]');
      const del = e.target.closest('[data-delete-product]');
      if (edit) openForm(edit.dataset.editProduct);
      if (del) run(async () => {
        const p = products.find((row) => String(row.id) === del.dataset.deleteProduct);
        if (!confirm(`Excluir o produto "${p?.name}"?`)) return;
        await api(`/Produto/${p.id}`, { method: 'DELETE' });
        products = await loadProducts();
        toast('Produto excluído.');
        draw();
      });
    });

    $('#newProduct').addEventListener('click', () => openForm());

    $('#productForm').addEventListener('submit', (e) => {
      e.preventDefault();
      run(async () => {
        const id = $('#productId').value;
        const body = {
          descricao: $('#productName').value.trim(),
          precoCompra: Number($('#productCost').value || 0),
          precoVenda: Number($('#productPrice').value || 0),
          qtde: Number($('#productStock').value || 0),
          idCategoria: Number($('#productCategory').value),
          estoqueMinimo: Number($('#productMin').value || 0)
        };

        if (id) await api(`/Produto/${id}`, { method: 'PUT', body });
        else await api('/Produto', { method: 'POST', body });

        products = await loadProducts();
        modal.classList.remove('open');
        toast('Produto salvo.');
        draw();
      });
    });

    search.addEventListener('input', draw);
    categoryFilter.addEventListener('change', draw);
    draw();
  }

  // Estoque: entrada manual (+), saída manual (−) ou acerto da quantidade. As vendas já baixam sozinhas.
  async function initStock() {
    if (!$('#stockPage')) return;

    const tbody = $('#stockBody');
    const modal = bindModal('#stockModal', null, ['#closeStockModal', '#cancelStock']);

    let products = await loadProducts();
    const categories = await loadCategories();

    $('#stockCategoryFilter').innerHTML = '<option value="">Todas as categorias</option>' +
      categories.map((c) => `<option value="${c.id}">${esc(c.name)}</option>`).join('');

    async function cards() {
      const r = await api('/Estoque/resumo');
      $('#stockTotal').textContent = r.totalProdutos;
      $('#stockLow').textContent = r.produtosBaixoEstoque;
      $('#stockZero').textContent = r.produtosSemEstoque;
      $('#stockValue').textContent = fmt(r.valorTotalEstoque);
      $('#stockCost').textContent = fmt(r.valorTotalCusto);
    }

    function draw() {
      const term = $('#stockSearch').value.trim().toLowerCase();
      const categoryId = $('#stockCategoryFilter').value;
      const status = $('#stockStatusFilter').value;

      const filtered = products.filter((p) => {
        const text = !term || p.name.toLowerCase().includes(term) || String(p.id) === term;
        const cat = !categoryId || String(p.categoryId) === String(categoryId);
        const st = !status || stockStatus(p).cls === status;
        return text && cat && st;
      });

      tbody.innerHTML = filtered.length ? filtered.map((p) => {
        const st = stockStatus(p);
        return `
          <tr>
            <td class="td-strong">#${p.id}</td>
            <td><strong>${esc(p.name)}</strong></td>
            <td>${esc(p.categoryName)}</td>
            <td>${fmt(p.price)}</td>
            <td><strong>${p.stock}</strong></td>
            <td>${p.minStock}</td>
            <td>${fmt(p.price * p.stock)}</td>
            <td><span class="status ${st.cls}">${st.label}</span></td>
            <td><button class="btn btn-secondary" data-adjust="${p.id}">Ajustar</button></td>
          </tr>`;
      }).join('') : '<tr><td colspan="9"><div class="empty">Nenhum produto encontrado.</div></td></tr>';
    }

    tbody.addEventListener('click', (e) => {
      const btn = e.target.closest('[data-adjust]');
      if (!btn) return;
      const p = products.find((row) => String(row.id) === btn.dataset.adjust);
      $('#stockProductId').value = p.id;
      $('#stockModalTitle').textContent = `Ajustar estoque · #${p.id} ${p.name}`;
      $('#stockCurrent').textContent = `Estoque atual: ${p.stock} un`;
      $('#stockMode').value = 'add';
      $('#stockQty').value = '';
      modal.classList.add('open');
    });

    $('#stockForm').addEventListener('submit', (e) => {
      e.preventDefault();
      run(async () => {
        const p = products.find((row) => String(row.id) === $('#stockProductId').value);
        const qty = Math.floor(Number($('#stockQty').value));
        const mode = $('#stockMode').value;

        if (!Number.isFinite(qty) || qty < 0) throw new Error('Informe uma quantidade válida.');

        let stock = qty;
        if (mode === 'add') stock = p.stock + qty;
        if (mode === 'remove') stock = p.stock - qty;
        if (stock < 0) throw new Error('A saída é maior que o estoque atual.');

        await api(`/Produto/${p.id}`, { method: 'PUT', body: productBody({ ...p, stock }) });

        products = await loadProducts();
        modal.classList.remove('open');
        toast('Estoque atualizado.');
        draw();
        await cards();
      });
    });

    ['#stockSearch'].forEach((id) => $(id).addEventListener('input', draw));
    ['#stockCategoryFilter', '#stockStatusFilter'].forEach((id) => $(id).addEventListener('change', draw));

    draw();
    await cards();
  }

  async function initCustomers() {
    if (!$('#customersPage')) return;

    const grid = $('#customersGrid');
    const search = $('#customerSearch');
    const modal = bindModal('#customerModal', null, ['#closeCustomerModal', '#cancelCustomer']);

    let customers = await loadCustomers();

    function draw() {
      const term = search.value.trim().toLowerCase();
      const digits = onlyDigits(term);

      const filtered = customers.filter((c) =>
        !term || c.name.toLowerCase().includes(term) || c.phone.toLowerCase().includes(term) ||
        (digits && onlyDigits(c.cpf).includes(digits)));

      const n = customers.length;
      $('#customerCount').textContent = `${n} cliente${n === 1 ? '' : 's'} cadastrado${n === 1 ? '' : 's'}`;

      grid.innerHTML = filtered.length ? filtered.map((c) => `
        <div class="card person-card">
          <h3>${esc(c.name)}</h3>
          <div class="muted">CPF ${esc(formatCpf(c.cpf))}</div>
          <div class="muted">${esc(c.phone) || 'Sem telefone'}</div>
          ${c.notes ? `<div class="muted" style="margin-top:6px">${esc(c.notes)}</div>` : ''}
          <div class="person-stats">
            <span>Compras: <strong>${c.purchases}</strong></span>
            <span>Total: <strong>${fmt(c.totalBought)}</strong></span>
            ${moduleOn('fiado') ? `<span>Fiado: <strong>${fmt(c.fiado)}</strong></span>` : ''}
          </div>
          ${isManager() ? `
            <div style="margin-top:12px;display:flex;gap:8px">
              <button class="btn btn-secondary" data-edit-customer="${c.id}">Editar</button>
              <button class="btn btn-danger" data-delete-customer="${c.id}">Excluir</button>
            </div>` : ''}
        </div>`).join('') : '<div class="empty" style="grid-column:1/-1">Nenhum cliente cadastrado.</div>';
    }

    function openForm(id = null) {
      const c = id ? customers.find((row) => String(row.id) === String(id)) : null;
      $('#customerModalTitle').textContent = c ? 'Editar cliente' : 'Novo cliente';
      $('#customerId').value = c?.id || '';
      $('#customerName').value = c?.name || '';
      $('#customerCpf').value = c ? formatCpf(c.cpf) : '';
      $('#customerPhone').value = c?.phone || '';
      $('#customerAddress').value = c?.address || '';
      $('#customerNotes').value = c?.notes || '';
      modal.classList.add('open');
    }

    grid.addEventListener('click', (e) => {
      const edit = e.target.closest('[data-edit-customer]');
      const del = e.target.closest('[data-delete-customer]');
      if (edit) openForm(edit.dataset.editCustomer);
      if (del) run(async () => {
        const c = customers.find((row) => String(row.id) === del.dataset.deleteCustomer);
        if (!confirm(`Excluir o cliente "${c?.name}"?`)) return;
        await api(`/Cliente/${c.id}`, { method: 'DELETE' });
        customers = await loadCustomers();
        toast('Cliente excluído.');
        draw();
      });
    });

    $('#newCustomer').addEventListener('click', () => openForm());

    $('#customerForm').addEventListener('submit', (e) => {
      e.preventDefault();
      run(async () => {
        const cpf = onlyDigits($('#customerCpf').value);
        if (cpf.length !== 11) throw new Error('O CPF é obrigatório e precisa ter 11 números.');

        const id = $('#customerId').value;
        const body = {
          nome: $('#customerName').value.trim(),
          cpf,
          telefone: $('#customerPhone').value.trim(),
          endereco: $('#customerAddress').value.trim(),
          observacao: $('#customerNotes').value.trim()
        };

        if (id) await api(`/Cliente/${id}`, { method: 'PUT', body });
        else await api('/Cliente', { method: 'POST', body });

        customers = await loadCustomers();
        modal.classList.remove('open');
        toast('Cliente salvo.');
        draw();
      });
    });

    search.addEventListener('input', draw);
    draw();
  }

  // Cadastros: só categorias por enquanto (fornecedores voltam com o módulo de compras)
  async function initRegisters() {
    if (!$('#registerPage')) return;

    async function drawCategories() {
      const categories = await loadCategories();

      $('#categoryList').innerHTML = categories.length ? categories.map((c) => `
        <div class="order-row"><div><strong>${esc(c.name)}</strong></div><button class="btn btn-secondary" data-remove-category="${c.id}">Remover</button></div>
      `).join('') : '<div class="empty">Nenhuma categoria cadastrada.</div>';
    }

    $('#categoryList').addEventListener('click', (e) => {
      const btn = e.target.closest('[data-remove-category]');
      if (!btn) return;
      run(async () => {
        await api(`/Categoria/${btn.dataset.removeCategory}`, { method: 'DELETE' });
        toast('Categoria removida.');
        await drawCategories();
      });
    });

    $('#categoryForm').addEventListener('submit', (e) => {
      e.preventDefault();
      run(async () => {
        await api('/Categoria', { method: 'POST', body: { descricao: $('#categoryName').value.trim() } });
        e.target.reset();
        toast('Categoria cadastrada.');
        await drawCategories();
      });
    });

    await drawCategories();
  }

  async function initSales() {
    if (!$('#salesPage')) return;

    let cart = [];
    let payment = 'Pix';
    let products = await loadProducts();
    const customers = await loadCustomers();
    const search = $('#saleProductSearch');
    const doneModal = $('#doneModal');
    let lastSaleId = null;

    const getProduct = (id) => products.find((p) => String(p.id) === String(id));
    const stockOf = (id) => getProduct(id)?.stock || 0;

    $('#saleCustomer').innerHTML = '<option value="">Sem cliente</option>' +
      customers.map((c) => `<option value="${c.id}">${esc(c.name)} · ${esc(formatCpf(c.cpf))}</option>`).join('');

    $('#payGrid').innerHTML = paymentOptions().map((name, i) =>
      `<button class="pay-btn ${i === 0 ? 'active' : ''}" data-payment="${name}">${name}</button>`).join('');

    function drawProducts() {
      const term = search.value.trim().toLowerCase();
      const list = products.filter((p) => !term || p.name.toLowerCase().includes(term) || String(p.id) === term);

      $('#saleProducts').innerHTML = list.length ? list.map((p) => `
        <button class="product-tile ${p.stock <= 0 ? 'disabled' : ''}" data-add-product="${p.id}">
          <span class="stock-chip">${p.stock} un</span>
          <strong>${esc(p.name)}</strong>
          <div class="muted">#${p.id}</div>
          <div class="product-price">${fmt(p.price)}</div>
        </button>
      `).join('') : '<div class="empty" style="grid-column:1/-1">Nenhum produto cadastrado.</div>';
    }

    function totals() {
      const subtotal = cart.reduce((sum, i) => sum + i.qty * i.price, 0);
      // O desconto não pode passar do subtotal (a API também confere)
      const discount = Math.min(Math.max(0, Number($('#saleDiscount').value || 0)), subtotal);
      return { subtotal, discount, total: subtotal - discount };
    }

    function drawCart() {
      $('#cartList').innerHTML = cart.length ? cart.map((i) => `
        <div class="cart-item">
          <div class="cart-row">
            <div><strong>${esc(getProduct(i.productId)?.name || 'Produto')}</strong><div class="muted">${fmt(i.price)} / un</div></div>
            <button class="btn btn-secondary" data-remove-cart="${i.productId}">Remover</button>
          </div>
          <div class="cart-row">
            <div class="qty"><button data-cart-minus="${i.productId}">−</button><span>${i.qty}</span><button data-cart-plus="${i.productId}">+</button></div>
            <strong>${fmt(i.qty * i.price)}</strong>
          </div>
        </div>
      `).join('') : '<div class="empty">Carrinho vazio.</div>';

      const t = totals();
      $('#saleSubtotal').textContent = fmt(t.subtotal);
      $('#saleDiscountValue').textContent = `- ${fmt(t.discount)}`;
      $('#saleTotal').textContent = fmt(t.total);
      $('#finishSale').textContent = `Finalizar venda · ${fmt(t.total)}`;
      $('#cartCount').textContent = `${cart.reduce((sum, i) => sum + i.qty, 0)} item(ns) no carrinho`;
    }

    $('#saleProducts').addEventListener('click', (e) => {
      const btn = e.target.closest('[data-add-product]');
      if (!btn) return;

      const product = getProduct(btn.dataset.addProduct);
      if (!product || product.stock <= 0) return;

      const line = cart.find((i) => String(i.productId) === String(product.id));
      if (!line) cart.push({ productId: product.id, qty: 1, price: product.price });
      else if (line.qty < product.stock) line.qty += 1;

      drawCart();
    });

    $('#cartList').addEventListener('click', (e) => {
      const remove = e.target.closest('[data-remove-cart]');
      const minus = e.target.closest('[data-cart-minus]');
      const plus = e.target.closest('[data-cart-plus]');
      const lineOf = (id) => cart.find((r) => String(r.productId) === String(id));

      if (remove) cart = cart.filter((i) => String(i.productId) !== remove.dataset.removeCart);

      if (minus) {
        const line = lineOf(minus.dataset.cartMinus);
        if (line) {
          line.qty -= 1;
          if (line.qty <= 0) cart = cart.filter((i) => i !== line);
        }
      }

      if (plus) {
        const line = lineOf(plus.dataset.cartPlus);
        if (line && line.qty < stockOf(line.productId)) line.qty += 1;
      }

      drawCart();
    });

    $('#payGrid').addEventListener('click', (e) => {
      const btn = e.target.closest('.pay-btn');
      if (!btn) return;
      $$('.pay-btn').forEach((b) => b.classList.remove('active'));
      btn.classList.add('active');
      payment = btn.dataset.payment;
    });

    $('#saleDiscount').addEventListener('input', drawCart);
    search.addEventListener('input', drawProducts);

    $('#finishSale').addEventListener('click', () => run(async () => {
      if (!cart.length) throw new Error('Adicione pelo menos um produto.');

      const customerId = $('#saleCustomer').value;
      if (payment === 'Fiado' && !customerId) throw new Error('Escolha o cliente para vender no fiado.');

      const t = totals();

      const sale = await api('/Venda', {
        method: 'POST',
        body: {
          clienteId: customerId ? Number(customerId) : null,
          formaPagamento: payment,
          desconto: t.discount,
          itens: cart.map((i) => ({ produtoId: i.productId, quantidade: i.qty }))
        }
      });

      lastSaleId = sale.idVenda;
      $('#doneInfo').innerHTML = `Venda <strong>#${sale.idVenda}</strong> registrada.<br>
        Total ${fmt(sale.totalVenda)}${sale.totalDesconto ? ` <span class="muted">(bruto ${fmt(sale.totalBruto)} − desconto ${fmt(sale.totalDesconto)})</span>` : ''}
        · ${esc(sale.formaPagamento)}`;
      doneModal.classList.add('open');

      cart = [];
      $('#saleDiscount').value = 0;
      products = await loadProducts();
      drawProducts();
      drawCart();
    }));

    $('#donePdf').addEventListener('click', () => run(async () => {
      await downloadPdf(`/Comprovante/venda/${lastSaleId}`, `comprovante_venda_${lastSaleId}.pdf`);
      toast('Comprovante baixado.');
    }));
    $('#doneClose').addEventListener('click', () => doneModal.classList.remove('open'));

    drawProducts();
    drawCart();
  }

  async function initHistory() {
    if (!$('#historyPage')) return;

    let range = '30';
    const sales = await loadSales();
    const modal = bindModal('#saleModal', null, ['#closeSaleModal', '#closeSaleModal2']);
    let openedId = null;

    $('#historyPayment').innerHTML = '<option value="">Todos</option>' +
      paymentOptions().map((p) => `<option>${p}</option>`).join('');

    function draw() {
      const exactDate = $('#historyDate').value;
      const term = $('#historySearch').value.trim().toLowerCase();
      const payment = $('#historyPayment').value;

      let list = exactDate ? sales.filter((s) => s.date === exactDate) : filterByRange(sales, range);

      list = list.filter((s) => {
        const names = s.items.map((i) => i.name).join(' ');
        const text = `#${s.id} ${s.customerName} ${names}`.toLowerCase();
        return (!term || text.includes(term)) && (!payment || s.payment.includes(payment));
      }).sort((a, b) => `${b.date} ${b.time}`.localeCompare(`${a.date} ${a.time}`));

      const net = list.reduce((sum, s) => sum + s.total, 0);
      const gross = list.reduce((sum, s) => sum + s.gross, 0);
      $('#historySummary').textContent =
        `${list.length} venda${list.length === 1 ? '' : 's'} • líquido ${fmt(net)} • bruto ${fmt(gross)}`;

      $('#historyBody').innerHTML = list.length ? list.map((s) => {
        const items = s.items.reduce((sum, i) => sum + i.qty, 0);
        return `
          <tr>
            <td class="td-strong">#${s.id}</td>
            <td>${displayDate(s.date)}</td>
            <td>${s.time || '—'}</td>
            <td>${esc(s.customerName) || '—'}</td>
            <td>${items}</td>
            <td><span class="status ${s.isFiado ? 'warn' : 'good'}">${esc(s.payment)}</span></td>
            <td>${fmt(s.gross)}</td>
            <td>${s.discount ? '- ' + fmt(s.discount) : '—'}</td>
            <td class="td-strong">${fmt(s.total)}</td>
            <td><button class="btn btn-secondary" data-sale="${s.id}">Detalhes</button></td>
          </tr>`;
      }).join('') : '<tr><td colspan="10"><div class="empty">Nenhuma venda encontrada.</div></td></tr>';
    }

    $('#historyBody').addEventListener('click', (e) => {
      const btn = e.target.closest('[data-sale]');
      if (!btn) return;

      const s = sales.find((row) => String(row.id) === btn.dataset.sale);
      openedId = s.id;
      $('#saleModalTitle').textContent = `Venda #${s.id} · ${displayDate(s.date)} ${s.time}`;
      $('#saleModalBody').innerHTML = `
        <div class="muted" style="margin-bottom:10px">Cliente: ${esc(s.customerName) || '—'} · Pagamento: ${esc(s.payment)}</div>
        <table class="table">
          <thead><tr><th>Produto</th><th>Qtd</th><th>Preço</th><th>Desconto</th><th>Total</th></tr></thead>
          <tbody>${s.items.map((i) => `
            <tr><td>${esc(i.name)}</td><td>${i.qty}</td><td>${fmt(i.price)}</td><td>${i.discount ? '- ' + fmt(i.discount) : '—'}</td><td class="td-strong">${fmt(i.total)}</td></tr>`).join('')}
          </tbody>
        </table>
        <div class="summary" style="margin-top:14px">
          <div class="summary-row"><span>Bruto</span><strong>${fmt(s.gross)}</strong></div>
          <div class="summary-row"><span>Desconto</span><strong>- ${fmt(s.discount)}</strong></div>
          <div class="summary-row total"><span>Líquido</span><strong>${fmt(s.total)}</strong></div>
        </div>`;
      modal.classList.add('open');
    });

    $('#saleModalPdf').addEventListener('click', () => run(() =>
      downloadPdf(`/Comprovante/venda/${openedId}`, `comprovante_venda_${openedId}.pdf`)));

    $$('.segment[data-history-range]').forEach((btn) => btn.addEventListener('click', () => {
      $$('.segment[data-history-range]').forEach((b) => b.classList.remove('active'));
      btn.classList.add('active');
      range = btn.dataset.historyRange;
      $('#historyDate').value = '';
      draw();
    }));

    $('#historyDate').addEventListener('change', () => {
      $$('.segment[data-history-range]').forEach((b) => b.classList.remove('active'));
      draw();
    });
    $('#historySearch').addEventListener('input', draw);
    $('#historyPayment').addEventListener('change', draw);
    draw();
  }

  // Fiados (módulo): quem deve, vendas pendentes, recebimento com recibo em PDF e histórico por cliente
  async function initFiados() {
    if (!$('#fiadoPage')) return;

    const modal = bindModal('#fiadoHistoryModal', null, ['#closeFiadoHistory', '#closeFiadoHistory2']);

    async function load() {
      const r = await api('/Fiado/resumo');
      const sales = r.vendasFiadas || [];

      $('#fiadoTotal').textContent = fmt(r.totalAReceber);
      $('#fiadoDebtors').textContent = r.quantidadeClientesDevedores;
      $('#fiadoPending').textContent = r.quantidadeVendasPendentes;
      $('#fiadoToday').textContent = fmt(r.totalRecebidoHoje);

      // Clientes com débito (soma das vendas pendentes de cada um)
      const byClient = {};
      sales.forEach((s) => {
        const row = (byClient[s.idCliente] ||= { id: s.idCliente, name: s.nomeCliente, total: 0, count: 0 });
        row.total += s.valorPendente;
        row.count += 1;
      });

      const debtors = Object.values(byClient).sort((a, b) => b.total - a.total);
      $('#debtorList').innerHTML = debtors.length ? debtors.map((d) => `
        <div class="order-row">
          <div><strong>${esc(d.name)}</strong><div class="muted">${d.count} venda${d.count === 1 ? '' : 's'} em aberto</div></div>
          <div class="order-actions"><strong>${fmt(d.total)}</strong>
            <button class="btn btn-secondary" data-history="${d.id}" data-name="${esc(d.name)}">Histórico</button></div>
        </div>`).join('') : '<div class="empty">Nenhum débito pendente.</div>';

      $('#pendingList').innerHTML = sales.length ? sales.map((s) => `
        <div class="order-row">
          <div>
            <strong>Venda #${s.idVenda} · ${esc(s.nomeCliente)}</strong>
            <div class="muted">${displayDate(s.dataHora)} · total ${fmt(s.valorTotal)} · pendente <strong>${fmt(s.valorPendente)}</strong></div>
          </div>
          <div class="order-actions">
            <input class="input" type="number" min="0.01" step="0.01" max="${s.valorPendente}" value="${s.valorPendente}" id="pay_${s.idVenda}" style="width:110px">
            <select class="select" id="payForm_${s.idVenda}" style="width:110px">${PAYMENTS.map((p) => `<option>${p}</option>`).join('')}</select>
            <button class="btn btn-primary" data-pay="${s.idVenda}" data-pending="${s.valorPendente}">Receber</button>
          </div>
        </div>`).join('') : '<div class="empty">Nenhuma venda fiada pendente.</div>';
    }

    $('#pendingList').addEventListener('click', (e) => {
      const btn = e.target.closest('[data-pay]');
      if (!btn) return;

      run(async () => {
        const idVenda = Number(btn.dataset.pay);
        const pending = Number(btn.dataset.pending);
        const valorPago = Number($(`#pay_${idVenda}`).value);

        if (!valorPago || valorPago <= 0) throw new Error('Informe um valor válido.');
        if (valorPago > pending) throw new Error(`O valor passa do débito (${fmt(pending)}).`);

        const result = await api('/Fiado/pagar', {
          method: 'POST',
          body: { idVenda, valorPago, formaPagamento: $(`#payForm_${idVenda}`).value }
        });

        toast(result.mensagem || 'Pagamento registrado.');

        // Recibo em PDF do pagamento
        await downloadPdf(`/Comprovante/pagamento/${idVenda}?valorPago=${valorPago}&saldoAnterior=${pending}`,
          `recibo_pagamento_venda_${idVenda}.pdf`);

        await load();
      });
    });

    $('#debtorList').addEventListener('click', (e) => {
      const btn = e.target.closest('[data-history]');
      if (!btn) return;

      run(async () => {
        const h = await api(`/Fiado/historico/${btn.dataset.history}`);
        $('#fiadoHistoryTitle').textContent = `Histórico · ${h.nomeCliente}`;
        $('#fiadoHistTotal').textContent = fmt(h.totalGeral);
        $('#fiadoHistPaid').textContent = fmt(h.totalPago);
        $('#fiadoHistBalance').textContent = fmt(h.saldoAtual);

        $('#fiadoHistoryBody').innerHTML = h.transacoes.length ? h.transacoes.map((t) => `
          <tr>
            <td>${displayDate(t.data)}</td>
            <td><span class="status ${t.tipo === 'PAGAMENTO' ? 'good' : 'warn'}">${esc(t.tipo)}</span></td>
            <td>${esc(t.descricao)}</td>
            <td>${fmt(t.valor)}</td>
            <td class="td-strong">${fmt(t.saldoApos)}</td>
          </tr>`).join('') : '<tr><td colspan="5"><div class="empty">Sem movimentações.</div></td></tr>';

        modal.classList.add('open');
      });
    });

    await load();
  }

  const MODULE_LABELS = {
    fiado: ['Fiado', 'Vendas a prazo e recebimento dos pagamentos.']
  };

  async function initSettings() {
    if (!$('#settingsPage')) return;

    const toggle = $('#darkTheme');
    toggle.checked = Prefs.theme === 'dark';
    toggle.addEventListener('change', () => {
      Prefs.theme = toggle.checked ? 'dark' : 'light';
      applyTheme();
    });

    $('#userInfo').textContent = `${Session.username} · perfil ${Session.role}`;
    $('#logoutBtn').addEventListener('click', logout);

    // Módulos: todos veem, só o Administrador liga e desliga
    const list = await api('/Modulos');
    const admin = Session.role === 'Administrador';

    $('#modulesList').innerHTML = list.length ? list.map((m) => {
      const [label, text] = MODULE_LABELS[m.nome] || [m.nome, m.descricao || ''];
      return `
        <div class="setting-row">
          <div><strong>${esc(label)}</strong><div class="muted">${esc(text)}</div></div>
          <label class="switch">
            <input type="checkbox" data-module="${esc(m.nome)}" ${m.ativo ? 'checked' : ''} ${admin ? '' : 'disabled'}>
            <span></span>
          </label>
        </div>`;
    }).join('') : '<div class="empty">Nenhum módulo instalado.</div>';

    $('#modulesList').addEventListener('change', (e) => {
      const input = e.target.closest('[data-module]');
      if (!input) return;

      run(async () => {
        try {
          await api(`/Modulos/${input.dataset.module}`, { method: 'PUT', body: { ativo: input.checked } });
        } catch (error) {
          input.checked = !input.checked;
          throw error;
        }
        toast('Módulo atualizado.');
        setTimeout(() => location.reload(), 600);   // refaz o menu com o módulo ligado/desligado
      });
    });
  }

  /* ------------------------------- login ------------------------------- */
  // Etapa 1: usuário e senha -> token temporário. Etapa 2: código do app (ou cadastro do app no 1º acesso).
  function initLogin() {
    applyTheme();

    let tempToken = null;
    let setupKey = null;

    const screens = ['stepLogin', 'step2faCode', 'step2faSetup'];
    const show = (id) => screens.forEach((s) => ($('#' + s).style.display = s === id ? 'block' : 'none'));
    const showError = (message) => {
      $$('.login-error').forEach((el) => (el.textContent = message || ''));
    };

    const flash = sessionStorage.getItem('flash');
    if (flash) {
      sessionStorage.removeItem('flash');
      showError(flash);
    }

    // Já tem sessão válida: vai direto para o sistema
    if (Session.valid) {
      location.href = 'index.html';
      return;
    }

    const backToLogin = () => {
      tempToken = null;
      setupKey = null;
      $('#code2fa').value = '';
      $('#codeSetup').value = '';
      showError('');
      show('stepLogin');
    };

    $$('[data-back-login]').forEach((a) => a.addEventListener('click', (e) => {
      e.preventDefault();
      backToLogin();
    }));

    const finish = (session) => {
      Session.save(session);
      location.href = 'index.html';
    };

    // ETAPA 1
    $('#loginForm').addEventListener('submit', async (e) => {
      e.preventDefault();
      showError('');

      try {
        const data = await api('/Auth/login', {
          method: 'POST',
          noAuth: true,
          body: { username: $('#loginUser').value.trim(), senha: $('#loginPassword').value }
        });

        if (!data || !data.tokenTemporario) throw new Error('Resposta inesperada do servidor.');
        tempToken = data.tokenTemporario;

        if (data.doisFatoresConfigurado) {
          show('step2faCode');
          $('#code2fa').focus();
          return;
        }

        // Primeiro acesso: gera a chave e mostra o QR Code
        const config = await api('/Auth/2fa/configurar', {
          method: 'POST', noAuth: true, headers: { Authorization: `Bearer ${tempToken}` }
        });

        setupKey = config.chave;
        $('#setupKey').textContent = config.chave;
        $('#qrcode').innerHTML = '';
        if (typeof QRCode !== 'undefined') {
          new QRCode($('#qrcode'), { text: config.uriOtpAuth, width: 180, height: 180 });
        }
        show('step2faSetup');
        $('#codeSetup').focus();
      } catch (error) {
        showError(error.message);
      }
    });

    // ETAPA 2 (acessos seguintes)
    $('#twoFaForm').addEventListener('submit', async (e) => {
      e.preventDefault();
      showError('');

      try {
        const session = await api('/Auth/2fa/verificar', {
          method: 'POST', noAuth: true,
          headers: { Authorization: `Bearer ${tempToken}` },
          body: { codigo: $('#code2fa').value.trim() }
        });
        finish(session);
      } catch (error) {
        showError(error.message);
      }
    });

    // ETAPA 2 (primeiro acesso)
    $('#setupForm').addEventListener('submit', async (e) => {
      e.preventDefault();
      showError('');

      try {
        const session = await api('/Auth/2fa/ativar', {
          method: 'POST', noAuth: true,
          headers: { Authorization: `Bearer ${tempToken}` },
          body: { chave: setupKey, codigo: $('#codeSetup').value.trim() }
        });
        finish(session);
      } catch (error) {
        showError(error.message);
      }
    });
  }

  /* ------------------------------- início ------------------------------- */
  async function boot() {
    if ($('#loginForm')) return initLogin();

    // Sem sessão válida volta para o login (o servidor também confere o token em cada chamada)
    if (!Session.valid) {
      Session.clear();
      location.href = 'login.html';
      return;
    }

    await loadModules();
    if (!guardPage()) return;

    buildShell();

    const flash = sessionStorage.getItem('flash');
    if (flash) {
      sessionStorage.removeItem('flash');
      toast(flash);
    }

    await Promise.all([
      run(initDashboard), run(initProducts), run(initStock), run(initCustomers), run(initRegisters),
      run(initSales), run(initHistory), run(initFiados), run(initSettings)
    ]);
  }

  document.addEventListener('DOMContentLoaded', () => run(boot));
})();
