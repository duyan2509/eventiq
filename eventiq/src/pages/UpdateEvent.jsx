import React, { useEffect, useState } from 'react';
import { Form, Button, Anchor, Card, Steps } from 'antd';
import { useParams, useNavigate } from 'react-router-dom';
import axios from 'axios';
import { EditorState, RichUtils, convertToRaw, convertFromRaw } from 'draft-js';
import { useMessage } from '../hooks/useMessage';
import { eventAPI, bankAPI } from '../services/api';
import EventInfoForm from '../components/forms/EventInfoForm';
import EventAddressForm from '../components/forms/EventAddressForm';
import PaymentInfoForm from '../components/forms/PaymentInfoForm';
import TicketClassManager from '../components/ticket-class/TicketClassManager';
import SeatMapManager from '../components/SeatMapManager';
import EventItemManager from '../components/EventItemManager';
import SubmitEventStep from '../components/SubmitEventStep';

const { Link } = Anchor;

const styleMap = {
    'BOLD': { fontWeight: 'bold' },
    'ITALIC': { fontStyle: 'italic' },
    'UNDERLINE': { textDecoration: 'underline' },
};

const UpdateEvent = () => {
    const { orgId, eventId } = useParams();
    const navigate = useNavigate();
    const [eventForm] = Form.useForm();
    const [addressForm] = Form.useForm();
    const [paymentForm] = Form.useForm();
    const [currentStep, setCurrentStep] = useState(0);
    const { success, error, warning, info, contextHolder } = useMessage();
    const [loading, setLoading] = useState(false);
    const [event, setEvent] = useState(null);
    const [provinces, setProvinces] = useState([]);
    const [communes, setCommunes] = useState([]);
    const [selectedProvince, setSelectedProvince] = useState(null);
    const [banks, setBanks] = useState([]);
    const [lookupLoading, setLookupLoading] = useState(false);
    const [editorState, setEditorState] = useState(() => EditorState.createEmpty());

    // Load event data
    useEffect(() => {
        const fetchEvent = async () => {
            try {
                const eventData = await eventAPI.getEvent(eventId);
                setEvent(eventData);
                eventForm.setFieldsValue({
                    name: eventData.name,
                    banner: eventData.banner ? [{
                        uid: '-1',
                        name: 'Current banner',
                        status: 'done',
                        url: eventData.banner,
                    }] : [],
                });

                // Initialize editor state from saved content
                if (eventData.description) {
                    try {
                        const rawContent = JSON.parse(eventData.description);
                        const contentState = convertFromRaw(rawContent);
                        setEditorState(EditorState.createWithContent(contentState));
                    } catch (err) {
                        // If description is not in raw format, set as plain text
                        setEditorState(EditorState.createWithText(eventData.description.toString()));
                    }
                }
                if (eventData.eventAddress) {
                    setSelectedProvince(eventData.eventAddress.provinceCode);
                    addressForm.setFieldsValue({
                        provinceCode: eventData.eventAddress.provinceCode,
                        communeCode: eventData.eventAddress.communeCode,
                        detail: eventData.eventAddress.detail,
                    });
                }
                if (eventData.bankCode) {
                    paymentForm.setFieldsValue({
                        bankCode: eventData.bankCode,
                        accountNumber: eventData.accountNumber,
                        accountName: eventData.accountName,
                    });
                }
            } catch (err) {
                console.error(err);
                error('Failed to load event data');
            }
        };
        fetchEvent();
    }, [eventId]);

    // Load provinces
    useEffect(() => {
        const fetchProvinces = async () => {
            try {
                const res = await axios.get('https://production.cas.so/address-kit/2025-07-01/provinces');
                setProvinces(res.data.provinces);
            } catch (err) {
                console.error(err)
                error('Failed to load provinces');
            }
        };
        fetchProvinces();
    }, []);

    // Load communes when province changes
    useEffect(() => {
        if (selectedProvince) {
            const fetchCommunes = async () => {
                try {
                    const res = await axios.get(`https://production.cas.so/address-kit/2025-07-01/provinces/${selectedProvince}/communes`);
                    setCommunes(res.data.communes);
                } catch (err) {
                    console.error(err)
                    error('Failed to load communes');
                }
            };
            fetchCommunes();
        } else {
            setCommunes([]);
        }
    }, [selectedProvince]);

    // Load banks
    useEffect(() => {
        const fetchBanks = async () => {
            try {
                const res = await bankAPI.getBankList();
                setBanks(res.data);
            } catch (err) {
                error('Failed to load bank list');
            }
        };
        fetchBanks();
    }, []);

    const handleKeyCommand = (command) => {
        const newState = RichUtils.handleKeyCommand(editorState, command);
        if (newState) {
            setEditorState(newState);
            return 'handled';
        }
        return 'not-handled';
    };

    const onStyleClick = (style) => {
        setEditorState(RichUtils.toggleInlineStyle(editorState, style));
    };

    const handleEventSubmit = async (values) => {
        setLoading(true);
        try {
            const contentState = editorState.getCurrentContent();
            const rawContent = convertToRaw(contentState);
            const description = JSON.stringify(rawContent);

            const formData = new FormData();
            formData.append('Name', values.name);
            formData.append('Description', description);

            // Handle banner file
            if (values.banner?.[0]?.originFileObj) {
                formData.append('Banner', values.banner[0].originFileObj);
            }

            await eventAPI.updateEvent(eventId, formData);
            success('Event information updated successfully!');
        } catch (err) {
            error(err?.response?.data?.message || 'Failed to update event');
        } finally {
            setLoading(false);
        }
    };

    const handleAddressSubmit = async (values) => {
        setLoading(true);
        try {
            const province = provinces.find(p => p.code === values.provinceCode);
            const commune = communes.find(c => c.code === values.communeCode);
            const addressData = {
                ...values,
                provinceName: province?.name,
                communeName: commune?.name,
            };
            await eventAPI.updateEventAddress(eventId, addressData);
            success('Event address updated successfully!');
        } catch (err) {
            error(err?.response?.data?.message || 'Failed to update address');
        } finally {
            setLoading(false);
        }
    };

    const handleBankLookup = async () => {
        const values = paymentForm.getFieldsValue(['bankCode', 'accountNumber']);
        if (!values.bankCode || !values.accountNumber) {
            warning('Please select bank and enter account number');
            return;
        }

        setLookupLoading(true);
        try {
            const response = await bankAPI.lookupBankAccount(values.bankCode, values.accountNumber);
            if (response?.data?.ownerName) {
                paymentForm.setFieldsValue({ accountName: response.data.ownerName });
            } else {
                error('No account information found');
            }
        } catch (err) {
            error(err.message || 'Failed to lookup account information');
        } finally {
            setLookupLoading(false);
        }
    };

    const handlePaymentSubmit = async (values) => {
        setLoading(true);
        try {
            await eventAPI.updateEventPayment(eventId, values);
            success('Payment information updated successfully!');
        } catch (err) {

            error(err?.response?.data?.message || 'Failed to update payment information');
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="flex">
            {contextHolder}
            <div className="flex-grow max-w-3xl mx-auto py-8 px-4">
                <h1 className="text-2xl font-bold mb-6">Update Event</h1>
                <Steps
                    current={currentStep}
                    items={[
                        { title: 'Update Information' },
                        { title: 'Ticket Class' },
                        { title: 'Seat Map' },
                        { title: 'Event Item' },
                        { title: 'Submit' },
                    ]}
                    className="mb-8"
                />

                <div className="flex justify-between mt-8 mb-8">
                    <Button
                        disabled={currentStep === 0}
                        onClick={() => setCurrentStep((prev) => prev - 1)}
                    >
                        Previous
                    </Button>
                    <Button
                        type="primary"
                        disabled={currentStep === 4}
                        onClick={() => setCurrentStep((prev) => prev + 1)}
                    >
                        Next
                    </Button>
                </div>
                {currentStep === 0 && (
                    <>
                        <EventInfoForm
                            form={eventForm}
                            loading={loading}
                            editorState={editorState}
                            setEditorState={setEditorState}
                            handleKeyCommand={handleKeyCommand}
                            onStyleClick={onStyleClick}
                            onFinish={handleEventSubmit}
                            initialBannerUrl={event?.banner}
                        />
                        <EventAddressForm
                            form={addressForm}
                            loading={loading}
                            provinces={provinces}
                            communes={communes}
                            selectedProvince={selectedProvince}
                            onProvinceChange={setSelectedProvince}
                            onFinish={handleAddressSubmit}
                        />
                        <PaymentInfoForm
                            form={paymentForm}
                            loading={loading}
                            lookupLoading={lookupLoading}
                            banks={banks}
                            onLookupClick={handleBankLookup}
                            onFinish={handlePaymentSubmit}
                        />
                    </>
                )}
                {currentStep === 1 && (
                    <TicketClassManager eventId={eventId} />
                )}
                {currentStep === 2 && (
                    <Card className="mb-8">
                        <SeatMapManager eventId={eventId} orgId={orgId} />
                    </Card>
                )}
                {currentStep === 3 && (
                    <Card className="mb-8">
                        <EventItemManager eventId={eventId} orgId={orgId} />
                    </Card>
                )}
                {currentStep === 4 && (
                    <Card className="mb-8">
                        <h2 className="text-xl font-bold mb-4">Validate & Submit Event</h2>
                        <SubmitEventStep
                            eventId={eventId}
                            onValidationChange={setLoading}
                        />
                    </Card>
                )}

                <div className="flex justify-between mt-8">
                    <Button
                        disabled={currentStep === 0}
                        onClick={() => setCurrentStep((prev) => prev - 1)}
                    >
                        Previous
                    </Button>
                    <Button
                        type="primary"
                        disabled={currentStep === 4}
                        onClick={() => setCurrentStep((prev) => prev + 1)}
                    >
                        Next
                    </Button>
                </div>
            </div>

        </div>
    );
};

export default UpdateEvent;