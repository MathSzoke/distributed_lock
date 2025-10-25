import React, { useMemo, useState } from "react";
import {
    Button,
    Card,
    CardHeader,
    CardPreview,
    Switch,
    Title1,
    Select,
    Input,
    makeStyles,
    tokens,
    Persona,
    InfoLabel,
    Field,
    Text
} from "@fluentui/react-components";

const API_BASE =
    import.meta.env.VITE_API_BASE?.replace(/\/+$/, "") || "http://localhost:8080";

const useStyles = makeStyles({
    titleDesc: {
        gap: "16px",
        display: "flex",
        flexDirection: "column",
        alignItems: "stretch",
    },
    container: {
        maxWidth: "920px",
        margin: "40px auto",
        padding: "0 16px"
    },
    controls: {
        display: "flex",
        gap: "12px",
        alignItems: "center",
        flexWrap: "wrap",
        marginBottom: "16px",
        marginTop: "2em"
    },
    select: {
        padding: "10px",
        border: `1px solid ${tokens.colorNeutralStroke1}`,
        borderRadius: "8px",
        minWidth: "180px"
    },
    mapWrap: {
        alignItems: "center",
        marginTop: "20px"
    },
    bar: {
        display: "flex",
        alignItems: "center",
        justifyContent: "space-between"
    },
    barContainer: {
        width: "100%"
    },
    node: {
        width: "24px",
        height: "24px",
        borderRadius: "999px",
        border: `2px solid ${tokens.colorNeutralStroke1}`
    },
    seg: {
        flex: 1,
        height: "2px",
        background: tokens.colorNeutralStroke1
    },
    track: {
        position: "relative",
        height: "74px"
    },
    personaA: {
        position: "absolute",
        top: "0px",
        transform: "translateX(-50%)",
        transition: "left 420ms cubic-bezier(.2,.8,.2,1)"
    },
    personaB: {
        position: "absolute",
        bottom: "0px",
        transform: "translateX(-50%)",
        transition: "left 420ms cubic-bezier(.2,.8,.2,1)"
    },
    grid: {
        display: "grid",
        gridTemplateColumns: "1fr 1fr",
        gap: "12px",
        marginTop: "20px"
    }
});

export default function App() {
    const s = useStyles();

    const [useLock, setUseLock] = useState(true);
    const [provider, setProvider] = useState("redis");
    const [busy, setBusy] = useState(false);
    const [workMs, setWorkMs] = useState(2000);
    const [lockTimeoutMs, setLockTimeoutMs] = useState(3000);

    const nodes = useMemo(() => [0, 50, 100], []);
    const [posA, setPosA] = useState(0);
    const [posB, setPosB] = useState(0);
    const [resA, setResA] = useState(null);
    const [resB, setResB] = useState(null);

    const x = { img: "https://randomuser.me/api/portraits/men/32.jpg" };
    const y = { img: "https://randomuser.me/api/portraits/women/61.jpg" };

    function stepIndex(step) {
        if (step === "Start") return 0;
        if (step === "Critical") return 1;
        if (step === "Done") return 2;
        return 0;
    }

    function animateToStep(setPos, step) {
        const idx = typeof step === "number" ? step : stepIndex(step);
        setPos(nodes[idx]);
    }

    async function runRace() {
        setBusy(true);
        setPosA(0);
        setPosB(0);
        setResA(null);
        setResB(null);

        const bodyBase = {
            useLock,
            provider,
            key: "order:demo",
            workMs,
            lockTimeoutMs
        };

        animateToStep(setPosA, 0);
        animateToStep(setPosB, 0);
        await new Promise(r => setTimeout(r, 180));
        animateToStep(setPosA, 1);
        animateToStep(setPosB, 1);

        const reqA = fetch(`${API_BASE}/api/run`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ ...bodyBase, actor: "João" })
        });

        await new Promise(r => setTimeout(r, 30));

        const reqB = fetch(`${API_BASE}/api/run`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ ...bodyBase, actor: "Maria" })
        });

        let aDone = false;
        let bDone = false;

        const handleA = reqA
            .then(async r => {
                const status = r.status;
                if (status === 409) return { status, message: r.data.message || r.message, data: null };
                const data = await r.json();
                return { status, data };
            })
            .then(res => {
                aDone = true;
                if (res.status === 200) animateToStep(setPosA, 2);
                setResA(res);
            });

        const handleB = reqB
            .then(async r => {
                const status = r.status;
                if (status === 409) return { status, message: r.data.message || r.message, data: null };
                const data = await r.json();
                return { status, data };
            })
            .then(res => {
                bDone = true;
                if (res.status === 200) animateToStep(setPosB, 2);
                setResB(res);
            });

        if (!useLock) {
            await Promise.race([handleA, handleB]);
            if (!aDone) await new Promise(r => setTimeout(r, 80));
            if (!bDone) await new Promise(r => setTimeout(r, 80));
        }

        await Promise.allSettled([handleA, handleB]);
        setBusy(false);
    }

    function LockInfo({ enabled }) {
        const onText = "Enables distributed lock. If two requests for the same key start together, the second waits or returns 409 on timeout.";
        const offText = "Disables lock. Two requests for the same key run concurrently and may conflict.";
        return (
            <InfoLabel info={enabled ? onText : offText}>
                {enabled ? "Lock enabled" : "Lock disabled"}
            </InfoLabel>
        );
    }

    function WorkLabelInfo() {
        return (
            <InfoLabel info={"This is how long your service take to finish their task in milliseconds"}>
                Work timer
            </InfoLabel>
        ); 
    }

    function ProviderLabelInfo() {
        return (
            <InfoLabel info={"This is what provider do you want to use. There's many ways to fix lock problems."}>
                Provider
            </InfoLabel>
        );
    }

    function LockTimeoutLabelInfo() {
        return (
            <InfoLabel
                info={
                    "Maximum time this request waits to acquire the distributed lock. If the lock isn’t acquired within this time, the request returns 409. Applies only when Lock is enabled."
                }
            >
                Lock timeout
            </InfoLabel>
        );
    }

    return (
        <div className={s.container}>
            <div className={s.titleDesc}>
                <Title1>Distributed Locking Demo</Title1>

                <Text align="justify">
                    This project demonstrates how to prevent race conditions in concurrent
                    operations using distributed locks. When two users trigger the same process
                    at the same time, the system coordinates access through Redis, Postgres, or
                    direct SQL advisory locks, ensuring that only one request runs the critical
                    section while the other waits or times out. It visualizes both concurrent
                    executions and how distributed locking guarantees data consistency and
                    thread safety in a multi-instance environment.
                </Text>

                <Text align="justify">
                    Although distributed locking is
                    effective, the most robust real-world solution combines idempotent request
                    design and database constraints to ensure data consistency even under
                    failure or retry scenarios.
                </Text>
            </div>

            <div className={s.controls}>
                <Switch
                    checked={useLock}
                    onChange={(_, d) => setUseLock(!!d.checked)}
                    label={<LockInfo enabled={useLock} />}
                />

                <Field
                    label={<ProviderLabelInfo />}
                >
                    <Select
                        className={s.select}
                        value={provider}
                        onChange={(e) => setProvider(e.target.value)}
                    >
                        <option value="redis">Redis</option>
                        <option value="postgres">Postgres</option>
                        <option value="dapper">Dapper (via SQL query)</option>
                    </Select>
                </Field>

                <Field
                    label={<WorkLabelInfo />}
                >
                    <Input
                        type="number"
                        inputMode="numeric"
                        min={100}
                        step={100}
                        value={String(workMs)}
                        onChange={(_, d) => setWorkMs(parseInt(d.value || "0"))}
                        placeholder="Work ms"
                        className={s.select}
                    />
                </Field>

                <Field label={<LockTimeoutLabelInfo />}>
                    <Input
                        type="number"
                        inputMode="numeric"
                        min={50}
                        step={50}
                        value={String(lockTimeoutMs)}
                        onChange={(_, d) => setLockTimeoutMs(parseInt(d.value || "0"))}
                        placeholder="Lock timeout ms"
                        className={s.select}
                        disabled={!useLock}
                    />
                </Field>

                <Button
                    appearance="primary"
                    disabled={busy}
                    onClick={runRace}
                >
                    Run test
                </Button>
            </div>

            <div className={s.mapWrap}>
                <div className={s.barContainer}>
                    <div className={s.track}>
                        <div
                            className={s.personaA}
                            style={{ left: `${posA}%` }}
                        >
                            <Persona
                                avatar={{ image: { src: x.img } }}
                                size="medium"
                                hidePersonaDetails
                            />
                        </div>

                        <div
                            className={s.personaB}
                            style={{ left: `${posB}%` }}
                        >
                            <Persona
                                avatar={{ image: { src: y.img } }}
                                size="medium"
                                hidePersonaDetails
                            />
                        </div>

                    </div>
                    <div className={s.bar}>
                        <div className={s.node}></div>
                        <div className={s.seg}></div>
                        <div className={s.node}></div>
                        <div className={s.seg}></div>
                        <div className={s.node}></div>
                    </div>
                </div>
            </div>

            {/*<div className={s.mapWrap}>*/}
            {/*    <div className={s.barContainer}>*/}
            {/*        <div className={s.track}>*/}
            {/*            <div*/}
            {/*                className={s.personaB}*/}
            {/*                style={{ left: `${posB}%` }}*/}
            {/*            >*/}
            {/*                <Persona*/}
            {/*                    avatar={{ image: { src: y.img } }}*/}
            {/*                    size="medium"*/}
            {/*                    hidePersonaDetails*/}
            {/*                />*/}
            {/*            </div>*/}
            {/*        </div>*/}
            {/*        <div className={s.bar}>*/}
            {/*            <div className={s.node}></div>*/}
            {/*            <div className={s.seg}></div>*/}
            {/*            <div className={s.node}></div>*/}
            {/*            <div className={s.seg}></div>*/}
            {/*            <div className={s.node}></div>*/}
            {/*        </div>*/}
            {/*    </div>*/}
            {/*</div>*/}

            <div className={s.grid}>
                <Card>
                    <CardHeader
                        header={
                            resA &&
                                <Persona
                                    avatar={{ image: { src: x.img } }}
                                    name={resA.data?.Actor}
                                    size="huge"
                                    hidePersonaDetails
                                />
                        } />
                    <CardPreview>
                        <pre
                            style={{
                                background: "#111",
                                color: "#0f0",
                                padding: 12,
                                borderRadius: 10,
                                overflow: "auto"
                            }}
                        >
                            {JSON.stringify(resA, null, 2)}
                        </pre>
                    </CardPreview>
                </Card>

                <Card>
                    <CardHeader
                        header={
                            resB &&
                                <Persona
                                    avatar={{ image: { src: y.img } }}
                                    name={resB.data?.Actor}
                                    size="huge"
                                    hidePersonaDetails
                                />
                        } />
                    <CardPreview>
                        <pre
                            style={{
                                background: "#111",
                                color: "#0f0",
                                padding: 12,
                                borderRadius: 10,
                                overflow: "auto",
                                width: 'unset'
                            }}
                        >
                            {JSON.stringify(resB, null, 2)}
                        </pre>
                    </CardPreview>
                </Card>
            </div>
        </div>
    );
}
