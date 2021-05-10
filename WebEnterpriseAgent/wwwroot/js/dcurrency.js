﻿import Web3 from "web3";
import TruffleContract from "truffle-contract";

//import $ from "jquery";
//import "bootstrap/js/src/util";
//import "./bootstrap/js/src/alert";

// If you change the name of contract, make sure you set the right reference here
import TutorialTokenArtifact from "../contracts/TIN.json";
//const require = createRequire(import.meta.url)
//const { TutorialTokenArtifact } = require("../contracts/TIN.json");

const App = {
    web3: null,
    web3Provider: null,
    contracts: {},

    init: function () {
        return App.initWeb3();
    },

    initWeb3: async function () {
        if (window.ethereum) {
            try {
                App.web3Provider = web3.currentProvider;
                App.web3 = new Web3(ethereum);
                await ethereum.enable();
            } catch (err) {
                console.error(err);
                App.showAlert(err.message, 'failed');
                return;
            }
        }
        // Initialize web3 and set the provider to the testRPC.
        else if (typeof web3 !== "undefined") {
            App.web3Provider = web3.currentProvider;
            App.web3 = new Web3(web3.currentProvider);
        } else {
            // set the provider you want from Web3.providers
            App.web3Provider = new Web3.providers.HttpProvider(
                "http://127.0.0.1:9545"
            );
            App.web3 = new Web3(App.web3Provider);
        }

        return App.initContract();
    },

    initContract: function () {
        // Get the necessary contract artifact file and instantiate it with truffle-contract.
        App.contracts.TutorialToken = TruffleContract(TutorialTokenArtifact);

        // Set the provider for our contract.
        App.contracts.TutorialToken.setProvider(App.web3Provider);

        // Use our contract to retieve and mark the adopted pets.
        App.getBalances();

        App.bindEvents();
    },

    bindEvents: function () {
        $(document).on("click", "#transfer-button", App.handleTransfer);
    },

    handleTransfer: function (event) {
        event.preventDefault();
        App.openLoading();

        const amount = parseInt($("#transfer-amount").val());
        const toAddress = $("#transfer-address").val();

        console.log("Transfer " + amount + " MT to " + toAddress);

        App.web3.eth.getAccounts(function (error, accounts) {
            if (error) {
                console.log(error);
                return;
            }

            var account = accounts[0];

            App.contracts.TutorialToken.deployed()
                .then(function (instance) {
                    return instance.transfer(toAddress, amount, {
                        from: account,
                        gas: 100000
                    });
                })
                .then(function (result) {
                    App.showAlert(`Transaction ${result.tx}`, 'success');

                    return App.getBalances();
                })
                .catch(function (err) {
                    console.log(err.message);
                    App.showAlert(err.message, 'failed');
                    App.closeloading();
                });
        });
    },

    getBalances: function () {
        console.log("Getting balances...");

        App.web3.eth.getAccounts(function (error, accounts) {
            if (error) {
                console.log(error);
                return;
            }

            const account = accounts[0];

            App.contracts.TutorialToken.deployed()
                .then(function (instance) {
                    return instance.balanceOf(account);
                })
                .then(function (result) {
                    const balance = result.toNumber();

                    $("#MY-balance").text(balance);
                })
                .catch(function (err) {
                    console.log(err.message);
                })
                .finally(function () {
                    App.closeloading();
                });
        });
    },

    closeloading: function () {
        $("#overlay")
            .removeClass("d-flex")
            .addClass("d-none");
    },

    openLoading: function () {
        $("#overlay")
            .removeClass("d-none")
            .addClass("d-flex");
    },

    showAlert: function (message, type) {
        const $content = $($(`#${type}-alert`).html());
        $content.find("#error-message").text(message);
        $content.appendTo("#alert-slot");
    }
};

$(window).on("load", function () {
    App.init();
});
