import React, { useEffect, useState } from 'react'

// MiniERP net10 Minimal API + JWT. Token lấy từ POST /api/auth/token, lưu localStorage, gửi Bearer.
const TOKEN_KEY = 'erp_token'

async function getToken(email, partnerCode, role) {
  const res = await fetch('/api/auth/token', {
    method: 'POST', headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, partnerCode, role })
  })
  const d = await res.json()
  if (!res.ok || !d.accessToken) throw new Error(d.error || 'Không lấy được token')
  return d.accessToken
}
async function authGet(path, token) {
  const res = await fetch(path, { headers: { Authorization: `Bearer ${token}` } })
  if (res.status === 401) throw new Error('unauthorized')
  const t = await res.text(); return t ? JSON.parse(t) : null
}
async function authPost(path, token, body) {
  const res = await fetch(path, { method: 'POST', headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' }, body: JSON.stringify(body) })
  const t = await res.text(); const d = t ? JSON.parse(t) : null
  if (!res.ok) throw new Error(d?.error || `Lỗi ${res.status}`)
  return d
}

function Login({ onLogin }) {
  const [f, setF] = useState({ email: 'admin@htc.vn', partnerCode: 'HTC', role: 'Admin' }); const [err, setErr] = useState(null); const [busy, setBusy] = useState(false)
  const up = (k, v) => setF({ ...f, [k]: v })
  const submit = async () => { setBusy(true); setErr(null); try { const t = await getToken(f.email.trim(), f.partnerCode.trim(), f.role.trim()); onLogin(t) } catch (e) { setErr(e.message) } finally { setBusy(false) } }
  return (
    <div className="wrap" style={{ maxWidth: 420, marginTop: 60 }}>
      <div className="card">
        <h1 style={{ textAlign: 'center', color: 'var(--brand)' }}>🏭 MiniERP</h1>
        <p className="muted" style={{ textAlign: 'center' }}>Cấp token JWT để dùng API (demo)</p>
        {err && <div className="flash err">{err}</div>}
        <label>Email</label><input value={f.email} onChange={e => up('email', e.target.value)} />
        <label>Mã đối tác</label><input value={f.partnerCode} onChange={e => up('partnerCode', e.target.value)} />
        <label>Vai trò</label><input value={f.role} onChange={e => up('role', e.target.value)} />
        <div style={{ marginTop: 14 }}><button className="btn" style={{ width: '100%' }} onClick={submit} disabled={busy}>{busy ? 'Đang cấp token…' : 'Vào hệ thống'}</button></div>
        <p className="muted" style={{ textAlign: 'center', fontSize: 12, marginTop: 12 }}>API docs (Scalar): <a href="/scalar/v1">/scalar/v1</a></p>
      </div>
    </div>
  )
}

function Dashboard({ token, onLogout }) {
  const [tab, setTab] = useState('dash')
  const [summary, setSummary] = useState(null); const [partners, setPartners] = useState([]); const [err, setErr] = useState(null); const [msg, setMsg] = useState(null)
  const [nf, setNf] = useState({ code: '', name: '', type: 'Dealer' })
  const load = () => {
    authGet('/api/report/dealer-summary', token).then(setSummary).catch(e => { if (e.message === 'unauthorized') onLogout(); else setErr(e.message) })
    authGet('/api/partners', token).then(d => setPartners(Array.isArray(d) ? d : (d?.items || []))).catch(() => {})
  }
  useEffect(() => { load() }, [])
  const addPartner = async () => {
    try { await authPost('/api/partners', token, { code: nf.code, name: nf.name, type: nf.type }); setMsg({ ok: true, text: 'Đã tạo đối tác' }); setNf({ code: '', name: '', type: 'Dealer' }); load() }
    catch (e) { setMsg({ ok: false, text: e.message }) }
  }
  return (
    <>
      <nav className="nav"><span className="brand">🏭 MiniERP</span>
        <a className={tab === 'dash' ? 'active' : ''} onClick={() => setTab('dash')} style={{ cursor: 'pointer' }}>Tổng quan</a>
        <a className={tab === 'partners' ? 'active' : ''} onClick={() => setTab('partners')} style={{ cursor: 'pointer' }}>Đối tác</a>
        <a href="/scalar/v1">API Docs</a>
        <a onClick={onLogout} style={{ marginLeft: 'auto', cursor: 'pointer' }}>Đăng xuất</a></nav>
      <div className="wrap">
        {err && <div className="flash err">{err}</div>}
        {tab === 'dash' && (
          <>
            <h1>Tổng quan đại lý</h1>
            {!summary ? <p className="muted">Đang tải…</p> : (
              <div className="card"><pre style={{ whiteSpace: 'pre-wrap', fontSize: 13 }}>{JSON.stringify(summary, null, 2)}</pre></div>
            )}
            <p className="muted">MiniERP là hệ API-first (.NET 10 Clean Architecture). Toàn bộ nghiệp vụ (Partner/Contract/Order/Inventory/Invoice/Report) khả dụng qua REST + tài liệu tương tác <a href="/scalar/v1">Scalar</a>.</p>
          </>
        )}
        {tab === 'partners' && (
          <>
            <h1>Đối tác</h1>
            {msg && <div className={`flash ${msg.ok ? 'ok' : 'err'}`}>{msg.text}</div>}
            <div className="card"><div className="row">
              <div style={{ flex: 1 }}><label>Mã</label><input value={nf.code} onChange={e => setNf({ ...nf, code: e.target.value })} /></div>
              <div style={{ flex: 1 }}><label>Tên</label><input value={nf.name} onChange={e => setNf({ ...nf, name: e.target.value })} /></div>
              <div style={{ flex: 1 }}><label>Loại</label><select value={nf.type} onChange={e => setNf({ ...nf, type: e.target.value })}><option>Dealer</option><option>Supplier</option><option>Customer</option></select></div>
              <div style={{ flex: 'none', alignSelf: 'flex-end' }}><button className="btn" onClick={addPartner}>+ Thêm</button></div>
            </div></div>
            <div className="card" style={{ padding: 0, overflow: 'auto' }}>
              <table><thead><tr><th>Mã</th><th>Tên</th><th>Loại</th></tr></thead>
                <tbody>{partners.map((p, i) => <tr key={i}><td>{p.code || p.Code}</td><td>{p.name || p.Name}</td><td>{p.type || p.Type}</td></tr>)}
                  {partners.length === 0 && <tr><td colSpan={3} className="muted" style={{ padding: 20 }}>Chưa có đối tác.</td></tr>}</tbody></table>
            </div>
          </>
        )}
      </div>
    </>
  )
}

export default function App() {
  const [token, setToken] = useState(() => localStorage.getItem(TOKEN_KEY))
  const onLogin = (t) => { localStorage.setItem(TOKEN_KEY, t); setToken(t) }
  const onLogout = () => { localStorage.removeItem(TOKEN_KEY); setToken(null) }
  return token ? <Dashboard token={token} onLogout={onLogout} /> : <Login onLogin={onLogin} />
}
