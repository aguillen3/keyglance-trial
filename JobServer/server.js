const http = require("http");
const fs = require("fs");
const path = require("path");
const crypto = require("crypto");

const PORT = Number(process.env.PORT || 3000);
const DATA = path.join(__dirname, "jobs.json");

function load() {
  if (!fs.existsSync(DATA)) return { jobs: [] };
  return JSON.parse(fs.readFileSync(DATA, "utf8"));
}
function save(db) {
  const tmp = DATA + "." + process.pid + ".tmp";
  fs.writeFileSync(tmp, JSON.stringify(db, null, 2));
  fs.renameSync(tmp, DATA);
}
function send(res, status, body) {
  res.writeHead(status, { "Content-Type": "application/json" });
  res.end(JSON.stringify(body));
}
function readJson(req) {
  return new Promise((resolve, reject) => {
    let body = "";
    req.on("data", c => {
      body += c;
      if (body.length > 1024 * 1024) req.destroy();
    });
    req.on("end", () => {
      try { resolve(body ? JSON.parse(body) : {}); }
      catch (e) { reject(e); }
    });
    req.on("error", reject);
  });
}

function validateJob(j) {
  return j && typeof j.id === "string" && j.id &&
    typeof j.client === "string" && typeof j.year === "number" &&
    typeof j.dueDate === "string" && j.fields && typeof j.fields === "object";
}

const server = http.createServer(async (req, res) => {
  try {
    if (req.method === "GET" && req.url === "/claim") {
      // This handler performs the read + claim synchronously in one Node event-loop turn.
      // Therefore two callers cannot interleave this critical section.
      const db = load();
      const candidates = db.jobs
        .filter(j => !j.claimedAt)
        .sort((a, b) => new Date(a.dueDate) - new Date(b.dueDate));
      if (!candidates.length) return send(res, 204, {});
      const job = candidates[0];
      job.claimedAt = new Date().toISOString();
      job.claimToken = crypto.randomUUID();
      save(db);
      return send(res, 200, {
        id: job.id, client: job.client, year: job.year,
        dueDate: job.dueDate, fields: job.fields, claimToken: job.claimToken
      });
    }

    const m = req.url.match(/^\/jobs\/([^/]+)\/result$/);
    if (req.method === "POST" && m) {
      const id = decodeURIComponent(m[1]);
      const body = await readJson(req);
      if (!["imported", "partial", "stopped"].includes(body.outcome))
        return send(res, 400, { error: "outcome must be imported, partial, or stopped" });

      const db = load();
      const job = db.jobs.find(j => j.id === id);
      if (!job) return send(res, 404, { error: "job not found" });
      if (!job.result) {
        job.result = {
          outcome: body.outcome,
          landedFields: Array.isArray(body.landedFields) ? body.landedFields : [],
          reason: body.reason || null,
          reportedAt: new Date().toISOString()
        };
        save(db);
      }
      return send(res, 200, job.result);
    }

    if (req.method === "POST" && req.url === "/jobs") {
      const job = await readJson(req);
      if (!validateJob(job)) return send(res, 400, { error: "invalid job" });
      const db = load();
      if (db.jobs.some(j => j.id === job.id))
        return send(res, 409, { error: "duplicate id" });
      db.jobs.push({ ...job, claimedAt: null, claimToken: null, result: null });
      save(db);
      return send(res, 201, job);
    }

    if (req.method === "GET" && req.url === "/jobs") {
      return send(res, 200, load().jobs);
    }

    send(res, 404, { error: "not found" });
  } catch (e) {
    console.error(e);
    send(res, 500, { error: "server error" });
  }
});

if (require.main === module) {
  if (!fs.existsSync(DATA)) save({ jobs: [] });
  server.listen(PORT, () => console.log(`JobServer listening on http://localhost:${PORT}`));
}
module.exports = { server, DATA };
